namespace Application.IdentityManagement.Services;

public interface IRefreshTokenService
{
    Task<string> GenerateAndStoreAsync(Guid utilisateurId, CancellationToken cancellationToken = default);
    Task<Guid?> ValidateAndRotateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid utilisateurId, CancellationToken cancellationToken = default);
}
