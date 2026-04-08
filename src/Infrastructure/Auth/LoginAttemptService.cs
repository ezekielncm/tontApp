namespace Infrastructure.Auth;

using Application.IdentityManagement.Services;
using StackExchange.Redis;

internal sealed class LoginAttemptService : ILoginAttemptService
{
    private readonly IConnectionMultiplexer _redis;

    private const string KeyPrefix = "login:attempts:";
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public LoginAttemptService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsLockedOutAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var attempts = await db.StringGetAsync(GetKey(telephone));

        if (!attempts.HasValue)
            return false;

        return (int)attempts >= MaxAttempts;
    }

    public async Task RegisterFailedAttemptAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(telephone);

        var newCount = await db.StringIncrementAsync(key);

        // Set TTL on first attempt or refresh on each attempt
        if (newCount == 1)
        {
            await db.KeyExpireAsync(key, LockoutDuration);
        }
    }

    public async Task ResetAttemptsAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(GetKey(telephone));
    }

    private static string GetKey(string telephone) => $"{KeyPrefix}{telephone}";
}
