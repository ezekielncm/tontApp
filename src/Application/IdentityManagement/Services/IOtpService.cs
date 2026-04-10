namespace Application.IdentityManagement.Services;

public interface IOtpService
{
    Task<string> GenerateAndStoreAsync(string telephone, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string telephone, string code, CancellationToken cancellationToken = default);
}
