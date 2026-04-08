namespace Application.PaymentManagement.Commands.InitierVersement;

using Application.Common;
using Domain.Common;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Ports;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class InitierVersementCommandHandler : ICommandHandler<InitierVersementCommand, Guid>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IMobileMoneyGateway _mobileMoneyGateway;
    private readonly IUnitOfWork _unitOfWork;

    public InitierVersementCommandHandler(
        IVersementRepository versementRepository,
        IMobileMoneyGateway mobileMoneyGateway,
        IUnitOfWork unitOfWork)
    {
        _versementRepository = versementRepository;
        _mobileMoneyGateway = mobileMoneyGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(InitierVersementCommand request, CancellationToken cancellationToken)
    {
        // Build the hash chain: get the last versement for this tontine
        var lastVersement = await _versementRepository.GetLastByTontineAsync(
            TontineId.From(request.TontineId), cancellationToken);
        var hashPrecedent = lastVersement?.HashCourant ?? string.Empty;

        var montant = Montant.Create(request.Montant, request.Devise);

        var versement = Versement.Create(
            TontineId.From(request.TontineId),
            TourId.From(request.TourId),
            PayeurId.From(request.PayeurId),
            montant,
            hashPrecedent);

        await _versementRepository.AddAsync(versement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Initiate mobile money payment (after persisting the versement)
        var mobileMoneyRequest = new MobileMoneyRequest(
            request.NumeroTelephone,
            montant.Valeur,
            montant.Devise,
            versement.Id.Value.ToString());

        await _mobileMoneyGateway.InitierPaiementAsync(mobileMoneyRequest, cancellationToken);

        return versement.Id.Value;
    }
}
