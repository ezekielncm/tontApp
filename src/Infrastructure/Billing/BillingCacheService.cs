namespace Infrastructure.Billing;

using Application.BillingManagement.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

/// <summary>
/// Redis-backed billing cache service for performant plan limit verification.
/// Caches plan limits and tontine counts per gestionnaire.
/// </summary>
internal sealed class BillingCacheService : IBillingCacheService
{
    private const string PlanLimitsPrefix = "billing:limits:";
    private const string TontineCountPrefix = "billing:tontine_count:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<BillingCacheService> _logger;

    public BillingCacheService(IConnectionMultiplexer redis, ILogger<BillingCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<PlanLimitsCache?> GetPlanLimitsAsync(
        string gestionnaireId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = PlanLimitsPrefix + gestionnaireId;

            var values = await db.HashGetAllAsync(key);
            if (values.Length == 0)
                return null;

            var dict = values.ToDictionary(
                v => v.Name.ToString(),
                v => v.Value.ToString());

            if (dict.TryGetValue("maxTontines", out var maxTontinesStr) &&
                dict.TryGetValue("maxMembres", out var maxMembresStr) &&
                int.TryParse(maxTontinesStr, out var maxTontines) &&
                int.TryParse(maxMembresStr, out var maxMembres))
            {
                return new PlanLimitsCache(maxTontines, maxMembres);
            }

            return null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error getting plan limits for {GestionnaireId}", gestionnaireId);
            return null;
        }
    }

    public async Task SetPlanLimitsAsync(
        string gestionnaireId, int maxTontines, int maxMembresParTontine,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = PlanLimitsPrefix + gestionnaireId;

            var entries = new HashEntry[]
            {
                new("maxTontines", maxTontines.ToString()),
                new("maxMembres", maxMembresParTontine.ToString())
            };

            await db.HashSetAsync(key, entries);
            await db.KeyExpireAsync(key, CacheDuration);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error setting plan limits for {GestionnaireId}", gestionnaireId);
        }
    }

    public async Task<int?> GetTontineCountAsync(
        string gestionnaireId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = TontineCountPrefix + gestionnaireId;

            var value = await db.StringGetAsync(key);
            if (value.IsNull)
                return null;

            return int.TryParse(value.ToString(), out var count) ? count : null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error getting tontine count for {GestionnaireId}", gestionnaireId);
            return null;
        }
    }

    public async Task IncrementTontineCountAsync(
        string gestionnaireId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = TontineCountPrefix + gestionnaireId;

            await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, CacheDuration);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error incrementing tontine count for {GestionnaireId}", gestionnaireId);
        }
    }

    public async Task SetTontineCountAsync(
        string gestionnaireId, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = TontineCountPrefix + gestionnaireId;

            await db.StringSetAsync(key, count.ToString(), CacheDuration);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error setting tontine count for {GestionnaireId}", gestionnaireId);
        }
    }

    public async Task InvalidateCacheAsync(
        string gestionnaireId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(PlanLimitsPrefix + gestionnaireId);
            await db.KeyDeleteAsync(TontineCountPrefix + gestionnaireId);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error invalidating cache for {GestionnaireId}", gestionnaireId);
        }
    }
}
