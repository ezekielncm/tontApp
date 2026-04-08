namespace Infrastructure.Auth;

using System.Security.Cryptography;
using Application.IdentityManagement.Services;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

internal sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _tokenExpiration;

    private const string TokenToUserPrefix = "refresh:token:";
    private const string UserToTokenPrefix = "refresh:user:";

    public RefreshTokenService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        var days = int.Parse(
            configuration.GetSection("Jwt")["RefreshTokenExpirationInDays"] ?? "30");
        _tokenExpiration = TimeSpan.FromDays(days);
    }

    public async Task<string> GenerateAndStoreAsync(Guid utilisateurId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var token = GenerateSecureToken();

        // Revoke any existing token for this user (rotation)
        var existingToken = await db.StringGetAsync(UserToTokenKey(utilisateurId));
        if (existingToken.HasValue)
        {
            await db.KeyDeleteAsync(TokenToUserKey((string)existingToken!));
        }

        // Store new token → userId mapping
        await db.StringSetAsync(TokenToUserKey(token), utilisateurId.ToString(), _tokenExpiration);
        // Store userId → token mapping (for revocation)
        await db.StringSetAsync(UserToTokenKey(utilisateurId), token, _tokenExpiration);

        return token;
    }

    public async Task<Guid?> ValidateAndRotateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        // Look up token → userId
        var userIdStr = await db.StringGetAsync(TokenToUserKey(refreshToken));
        if (!userIdStr.HasValue)
            return null;

        if (!Guid.TryParse(userIdStr.ToString(), out var utilisateurId))
            return null;

        // Invalidate the old token (rotation)
        await db.KeyDeleteAsync(TokenToUserKey(refreshToken));
        await db.KeyDeleteAsync(UserToTokenKey(utilisateurId));

        return utilisateurId;
    }

    public async Task RevokeAsync(Guid utilisateurId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        var existingToken = await db.StringGetAsync(UserToTokenKey(utilisateurId));
        if (existingToken.HasValue)
        {
            await db.KeyDeleteAsync(TokenToUserKey((string)existingToken!));
        }

        await db.KeyDeleteAsync(UserToTokenKey(utilisateurId));
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string TokenToUserKey(string token) => $"{TokenToUserPrefix}{token}";
    private static string UserToTokenKey(Guid userId) => $"{UserToTokenPrefix}{userId}";
}
