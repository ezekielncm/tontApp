namespace Domain.TontineManagement.Repositories;

using Domain.TontineManagement.ValueObjects;

public interface ITontineRepository
{
    Task<Tontine?> GetByIdAsync(TontineId id, CancellationToken cancellationToken = default);
    Task<Tontine?> GetByIdReadOnlyAsync(TontineId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tontine>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tontine>> GetByStatusReadOnlyAsync(TontineStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(Tontine tontine, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tontine tontine, CancellationToken cancellationToken = default);
}
