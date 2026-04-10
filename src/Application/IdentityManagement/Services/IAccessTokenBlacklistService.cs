namespace Application.IdentityManagement.Services;

public interface IAccessTokenBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}
