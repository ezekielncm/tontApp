namespace Application.IdentityManagement.Commands.Deconnecter;

using Application.Common;
using Application.IdentityManagement.Services;
using Microsoft.Extensions.Logging;

public sealed class DeconnecterCommandHandler : ICommandHandler<DeconnecterCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccessTokenBlacklistService _blacklistService;
    private readonly ILogger<DeconnecterCommandHandler> _logger;

    public DeconnecterCommandHandler(
        IRefreshTokenService refreshTokenService,
        IAccessTokenBlacklistService blacklistService,
        ILogger<DeconnecterCommandHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _blacklistService = blacklistService;
        _logger = logger;
    }

    public async Task Handle(DeconnecterCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAsync(request.UtilisateurId, cancellationToken);

        if (!string.IsNullOrEmpty(request.Jti))
        {
            // Blacklist the access token for its remaining TTL (max 15 min)
            await _blacklistService.BlacklistAsync(request.Jti, TimeSpan.FromMinutes(15), cancellationToken);
        }

        _logger.LogInformation("User {UserId} logged out", request.UtilisateurId);
    }
}
