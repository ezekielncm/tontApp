namespace Application.PaymentManagement.Queries.VerifierAudit;

using Application.Common;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class VerifierAuditQueryHandler
    : IQueryHandler<VerifierAuditQuery, AuditVerificationResult>
{
    private readonly IVersementRepository _versementRepository;

    public VerifierAuditQueryHandler(IVersementRepository versementRepository)
    {
        _versementRepository = versementRepository;
    }

    public async Task<AuditVerificationResult> Handle(
        VerifierAuditQuery request,
        CancellationToken cancellationToken)
    {
        var versements = await _versementRepository.GetByTontineAsync(
            TontineId.From(request.TontineId), cancellationToken);

        var details = new List<AuditVersementDetail>();
        var nombreValides = 0;

        foreach (var versement in versements)
        {
            var estValide = versement.VerifierIntegrite();
            if (estValide)
                nombreValides++;

            details.Add(new AuditVersementDetail(
                versement.Id.Value,
                estValide,
                versement.Statut.ToString(),
                versement.Montant.Valeur,
                versement.Montant.Devise,
                versement.CreatedAt));
        }

        return new AuditVerificationResult(
            EstValide: nombreValides == versements.Count,
            NombreVersements: versements.Count,
            NombreVersementsValides: nombreValides,
            Details: details);
    }
}
