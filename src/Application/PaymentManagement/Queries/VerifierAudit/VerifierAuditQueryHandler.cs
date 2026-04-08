namespace Application.PaymentManagement.Queries.VerifierAudit;

using Application.Common;
using Application.PaymentManagement.Services;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class VerifierAuditQueryHandler
    : IQueryHandler<VerifierAuditQuery, AuditVerificationResult>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IAuditTrailService _auditTrailService;

    public VerifierAuditQueryHandler(
        IVersementRepository versementRepository,
        IAuditTrailService auditTrailService)
    {
        _versementRepository = versementRepository;
        _auditTrailService = auditTrailService;
    }

    public async Task<AuditVerificationResult> Handle(
        VerifierAuditQuery request,
        CancellationToken cancellationToken)
    {
        var tontineId = TontineId.From(request.TontineId);

        // Verify the full audit chain at the tontine level
        var chainReport = await _auditTrailService.VerifierChaine(tontineId, cancellationToken);

        // Also verify per-versement integrity
        var versements = await _versementRepository.GetByTontineAsync(tontineId, cancellationToken);

        var details = new List<AuditVersementDetail>();
        foreach (var versement in versements)
        {
            var estValide = versement.VerifierIntegrite();
            details.Add(new AuditVersementDetail(
                versement.Id.Value,
                estValide,
                versement.Statut.ToString(),
                versement.Montant.Valeur,
                versement.Montant.Devise,
                versement.CreatedAt));
        }

        return new AuditVerificationResult(
            EstValide: chainReport.EstIntegre,
            NombreEntrees: chainReport.NombreEntrees,
            NombreEntreesValides: chainReport.NombreEntreesValides,
            NombreEntreesInvalides: chainReport.NombreEntreesInvalides,
            DerniereVerification: chainReport.VerificationTimestamp,
            DernierHash: chainReport.DernierHash,
            PremiereEntreeInvalideId: chainReport.PremiereEntreeInvalideId,
            Details: details);
    }
}
