namespace Application.IdentityManagement.Commands.Deconnecter;

using Application.Common;
using Application.IdentityManagement.Services;
using Microsoft.Extensions.Logging;

public sealed class DeconnecterCommandHandler : ICommandHandler<DeconnecterCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<DeconnecterCommandHandler> _logger;

    public DeconnecterCommandHandler(
        IRefreshTokenService refreshTokenService,
        ILogger<DeconnecterCommandHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task Handle(DeconnecterCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAsync(request.UtilisateurId, cancellationToken);

        _logger.LogInformation("User {UserId} logged out", request.UtilisateurId);
    }
}
