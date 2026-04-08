namespace Application.IdentityManagement.Commands.ConnecterUtilisateur;

using Application.Common;
using Application.IdentityManagement.DTOs;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class ConnecterUtilisateurCommandHandler
    : ICommandHandler<ConnecterUtilisateurCommand, AuthResult>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ILogger<ConnecterUtilisateurCommandHandler> _logger;

    public ConnecterUtilisateurCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILoginAttemptService loginAttemptService,
        ILogger<ConnecterUtilisateurCommandHandler> logger)
    {
        _utilisateurRepository = utilisateurRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _loginAttemptService = loginAttemptService;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(
        ConnecterUtilisateurCommand request,
        CancellationToken cancellationToken)
    {
        var telephoneId = TelephoneId.Create(request.Telephone);

        // Check lockout (5 failed attempts, 15min TTL)
        if (await _loginAttemptService.IsLockedOutAsync(telephoneId.Value, cancellationToken))
        {
            _logger.LogWarning("Login attempt blocked for locked account");
            throw new InvalidOperationException(
                "Compte temporairement verrouillé après trop de tentatives. Réessayez dans 15 minutes.");
        }

        var utilisateur = await _utilisateurRepository.GetByTelephoneAsync(
            telephoneId.Value, cancellationToken);

        if (utilisateur is null)
        {
            await _loginAttemptService.RegisterFailedAttemptAsync(telephoneId.Value, cancellationToken);
            _logger.LogWarning("Login failed: unknown telephone");
            throw new InvalidOperationException("Numéro de téléphone ou mot de passe incorrect.");
        }

        if (!utilisateur.EstActif)
        {
            _logger.LogWarning("Login attempt on deactivated account {UserId}", utilisateur.Id.Value);
            throw new InvalidOperationException("Ce compte a été désactivé.");
        }

        if (!_passwordHasher.Verify(request.MotDePasse, utilisateur.MotDePasseHash.Value))
        {
            await _loginAttemptService.RegisterFailedAttemptAsync(telephoneId.Value, cancellationToken);
            _logger.LogWarning("Login failed: invalid password for user {UserId}", utilisateur.Id.Value);
            throw new InvalidOperationException("Numéro de téléphone ou mot de passe incorrect.");
        }

        // Reset failed attempts on successful login
        await _loginAttemptService.ResetAttemptsAsync(telephoneId.Value, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(utilisateur);
        var refreshToken = await _refreshTokenService.GenerateAndStoreAsync(
            utilisateur.Id.Value, cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", utilisateur.Id.Value);

        return new AuthResult(
            utilisateur.Id.Value,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddHours(24));
    }
}
