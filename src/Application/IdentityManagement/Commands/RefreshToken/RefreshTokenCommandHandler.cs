namespace Application.IdentityManagement.Commands.RefreshToken;

using Application.Common;
using Application.IdentityManagement.DTOs;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _utilisateurRepository = utilisateurRepository;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Validate and rotate: old token is invalidated, returns userId
        var utilisateurId = await _refreshTokenService.ValidateAndRotateAsync(
            request.RefreshToken, cancellationToken);

        if (utilisateurId is null)
        {
            _logger.LogWarning("Invalid or expired refresh token used");
            throw new InvalidOperationException("Le refresh token est invalide ou expiré.");
        }

        var utilisateur = await _utilisateurRepository.GetByIdAsync(
            UtilisateurId.From(utilisateurId.Value), cancellationToken);

        if (utilisateur is null || !utilisateur.EstActif)
        {
            _logger.LogWarning("Refresh token used for non-existent or deactivated account");
            throw new InvalidOperationException("Le compte associé est introuvable ou désactivé.");
        }

        var accessToken = _jwtService.GenerateAccessToken(utilisateur);
        var newRefreshToken = await _refreshTokenService.GenerateAndStoreAsync(
            utilisateur.Id.Value, cancellationToken);

        _logger.LogInformation("Token refreshed for user {UserId}", utilisateur.Id.Value);

        return new AuthResult(
            utilisateur.Id.Value,
            accessToken,
            newRefreshToken,
            DateTime.UtcNow.AddHours(24));
    }
}
