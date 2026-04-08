namespace Application.PaymentManagement.Queries.VerifierAudit;

/// <summary>
/// Result of audit trail verification for a tontine.
/// Includes the full chain verification report and per-versement details.
/// </summary>
public sealed record AuditVerificationResult(
    bool EstValide,
    int NombreEntrees,
    int NombreEntreesValides,
    int NombreEntreesInvalides,
    DateTime DerniereVerification,
    string DernierHash,
    string? PremiereEntreeInvalideId,
    IReadOnlyList<AuditVersementDetail> Details);

public sealed record AuditVersementDetail(
    Guid VersementId,
    bool EstValide,
    string Statut,
    decimal Montant,
    string Devise,
    DateTime CreatedAt);
