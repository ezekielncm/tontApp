namespace Application.PaymentManagement.Queries.GetAuditEntries;

/// <summary>
/// DTO representing a single audit entry in the response.
/// </summary>
public sealed record AuditEntryDto(
    Guid Id,
    Guid VersementId,
    Guid TontineId,
    string Action,
    string ActeurId,
    DateTime Timestamp,
    string Payload,
    string HashPrecedent,
    string HashCourant);

/// <summary>
/// Paginated result of audit entries for a tontine.
/// </summary>
public sealed record AuditEntriesResult(
    IReadOnlyList<AuditEntryDto> Entries,
    int TotalCount,
    int Page,
    int PageSize);
