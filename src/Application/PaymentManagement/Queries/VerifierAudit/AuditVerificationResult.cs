namespace Application.PaymentManagement.Queries.VerifierAudit;

/// <summary>
/// Result of audit trail verification for a tontine's versements.
/// </summary>
public sealed record AuditVerificationResult(
    bool EstValide,
    int NombreVersements,
    int NombreVersementsValides,
    IReadOnlyList<AuditVersementDetail> Details);

public sealed record AuditVersementDetail(
    Guid VersementId,
    bool EstValide,
    string Statut,
    decimal Montant,
    string Devise,
    DateTime CreatedAt);
