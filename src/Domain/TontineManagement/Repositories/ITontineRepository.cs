namespace Domain.TontineManagement.Repositories;

using Domain.TontineManagement.ValueObjects;

public interface ITontineRepository
{
    Task<Tontine?> GetByIdAsync(TontineId id, CancellationToken cancellationToken = default);
    Task<Tontine?> GetByIdReadOnlyAsync(TontineId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tontine>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tontine>> GetByStatusReadOnlyAsync(TontineStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the tontine that contains an invitation matching the given code hash.
    /// Returns null if no matching invitation is found.
    /// </summary>
    Task<Tontine?> GetByInvitationCodeHashAsync(string codeHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of tontines created by a gestionnaire (non-cancelled).
    /// </summary>
    Task<int> CountByGestionnaireAsync(string gestionnaireId, CancellationToken cancellationToken = default);

    Task AddAsync(Tontine tontine, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tontine tontine, CancellationToken cancellationToken = default);
}
