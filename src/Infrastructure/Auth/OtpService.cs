namespace Infrastructure.Auth;

using System.Security.Cryptography;
using Application.IdentityManagement.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public sealed class OtpService : IOtpService
{
    private const string Prefix = "otp:";
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IConnectionMultiplexer redis, ILogger<OtpService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string> GenerateAndStoreAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"{Prefix}{telephone}", code, OtpTtl);
        _logger.LogInformation("OTP generated for {Telephone}", telephone);
        return code;
    }

    public async Task<bool> ValidateAsync(string telephone, string code, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"{Prefix}{telephone}";
        var stored = await db.StringGetAsync(key);

        if (stored.IsNullOrEmpty || stored != code)
            return false;

        // One-time use: delete after successful validation
        await db.KeyDeleteAsync(key);
        return true;
    }
}
