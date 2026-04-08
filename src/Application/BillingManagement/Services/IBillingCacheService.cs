namespace Application.BillingManagement.Services;

/// <summary>
/// Service for caching subscription plan limits in Redis.
/// Used by CheckAbonnementFilter for performant limit verification.
/// </summary>
public interface IBillingCacheService
{
    /// <summary>
    /// Gets the plan limits for a gestionnaire from Redis cache.
    /// Returns null if not cached (caller should fall back to DB).
    /// </summary>
    Task<PlanLimitsCache?> GetPlanLimitsAsync(string gestionnaireId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the plan limits for a gestionnaire in Redis cache.
    /// </summary>
    Task SetPlanLimitsAsync(string gestionnaireId, int maxTontines, int maxMembresParTontine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current tontine count for a gestionnaire from Redis cache.
    /// Returns null if not cached.
    /// </summary>
    Task<int?> GetTontineCountAsync(string gestionnaireId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the tontine count for a gestionnaire in Redis.
    /// </summary>
    Task IncrementTontineCountAsync(string gestionnaireId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the tontine count for a gestionnaire in Redis (for initialization).
    /// </summary>
    Task SetTontineCountAsync(string gestionnaireId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached billing data for a gestionnaire.
    /// </summary>
    Task InvalidateCacheAsync(string gestionnaireId, CancellationToken cancellationToken = default);
}

public sealed record PlanLimitsCache(
    int MaxTontines,
    int MaxMembresParTontine);
