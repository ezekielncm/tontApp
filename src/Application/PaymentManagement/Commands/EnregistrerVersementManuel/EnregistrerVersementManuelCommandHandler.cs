namespace Application.PaymentManagement.Commands.EnregistrerVersementManuel;

using System.Text.Json;
using Application.Common;
using Application.PaymentManagement.Services;
using Domain.Common;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class EnregistrerVersementManuelCommandHandler
    : ICommandHandler<EnregistrerVersementManuelCommand, Result>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IUnitOfWork _unitOfWork;

    public EnregistrerVersementManuelCommandHandler(
        IVersementRepository versementRepository,
        IAuditTrailService auditTrailService,
        IUnitOfWork unitOfWork)
    {
        _versementRepository = versementRepository;
        _auditTrailService = auditTrailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        EnregistrerVersementManuelCommand request,
        CancellationToken cancellationToken)
    {
        var tontineId = TontineId.From(request.TontineId);
        var tourId = TourId.From(request.TourId);
        var payeurId = PayeurId.From(request.MembreId);

        // Idempotence: reject if a confirmed versement already exists for this membre + tour
        var existingVersements = await _versementRepository.GetByTontineAndTourAsync(
            tontineId, tourId, cancellationToken);

        var alreadyConfirmed = existingVersements
            .Any(v => v.PayeurId == payeurId && v.Statut == VersementStatus.Confirme);

        if (alreadyConfirmed)
        {
            return Result.Failure(
                $"Un versement confirmé existe déjà pour le membre {request.MembreId} sur ce tour.");
        }

        // Build hash chain from last versement for this tontine
        var lastVersement = await _versementRepository.GetLastByTontineAsync(
            tontineId, cancellationToken);
        var hashPrecedent = lastVersement?.HashCourant ?? string.Empty;

        var montant = Montant.Create(request.Montant, request.Devise);

        // Create versement (EN_ATTENTE) then immediately confirm as CASH
        var versement = Versement.Create(
            tontineId,
            tourId,
            payeurId,
            montant,
            hashPrecedent);

        var referenceExterne = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.MembreId.ToString()[..8]}";
        versement.Confirmer(referenceExterne);

        await _versementRepository.AddAsync(versement, cancellationToken);

        // Add audit trail entry via existing service (same hash chain as Orange Money payments)
        var payload = JsonSerializer.Serialize(new
        {
            request.TontineId,
            request.TourId,
            request.MembreId,
            request.Montant,
            request.DescriptionPreuve,
            ReferenceExterne = referenceExterne
        });

        await _auditTrailService.AjouterEntree(
            tontineId,
            versement.Id,
            AuditAction.VersementManuel,
            request.MembreId.ToString(),
            payload,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
