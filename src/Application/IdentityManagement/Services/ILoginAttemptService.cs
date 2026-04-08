namespace Application.IdentityManagement.Services;

public interface ILoginAttemptService
{
    Task<bool> IsLockedOutAsync(string telephone, CancellationToken cancellationToken = default);
    Task RegisterFailedAttemptAsync(string telephone, CancellationToken cancellationToken = default);
    Task ResetAttemptsAsync(string telephone, CancellationToken cancellationToken = default);
}
