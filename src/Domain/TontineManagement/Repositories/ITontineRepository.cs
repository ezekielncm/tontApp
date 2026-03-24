namespace Domain.TontineManagement.Repositories;

using Domain.TontineManagement.ValueObjects;

public interface ITontineRepository
{
    Task<Tontine?> GetByIdAsync(TontineId id, CancellationToken cancellationToken = default);
    Task AddAsync(Tontine tontine, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tontine tontine, CancellationToken cancellationToken = default);
}
