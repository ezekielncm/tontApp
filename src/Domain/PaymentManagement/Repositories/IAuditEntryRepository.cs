namespace Domain.PaymentManagement.Repositories;

using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public interface IAuditEntryRepository
{
    /// <summary>
    /// Gets the last N audit entries for a tontine, ordered by timestamp descending (newest first).
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByTontinePagedAsync(
        TontineId tontineId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ALL audit entries for a tontine, ordered by timestamp ascending (oldest first) for chain verification.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByTontineOrderedAsync(
        TontineId tontineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last audit entry (most recent) for a tontine.
    /// </summary>
    Task<AuditEntry?> GetLastByTontineAsync(
        TontineId tontineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of audit entries for a tontine.
    /// </summary>
    Task<int> CountByTontineAsync(
        TontineId tontineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit entry to the store.
    /// </summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
