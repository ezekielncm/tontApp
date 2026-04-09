namespace Domain.PaymentManagement.ValueObjects;

/// <summary>
/// Enum representing the possible actions recorded in the audit trail.
/// </summary>
public enum AuditAction
{
    VersementCree,
    VersementConfirme,
    VersementRejete,
    VersementManuel
}
