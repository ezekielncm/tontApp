namespace Infrastructure.Auth;

using Application.IdentityManagement.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public sealed class AccessTokenBlacklistService : IAccessTokenBlacklistService
{
    private const string Prefix = "token:blacklist:";
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AccessTokenBlacklistService> _logger;

    public AccessTokenBlacklistService(
        IConnectionMultiplexer redis,
        ILogger<AccessTokenBlacklistService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"{Prefix}{jti}", "1", ttl);
        _logger.LogInformation("Access token {Jti} blacklisted for {Ttl}", jti, ttl);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"{Prefix}{jti}");
    }
}
