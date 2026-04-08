namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using Application.Common;
using Application.IdentityManagement.DTOs;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class InscrireUtilisateurCommandHandler
    : ICommandHandler<InscrireUtilisateurCommand, AuthResult>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<InscrireUtilisateurCommandHandler> _logger;

    public InscrireUtilisateurCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILogger<InscrireUtilisateurCommandHandler> logger)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(
        InscrireUtilisateurCommand request,
        CancellationToken cancellationToken)
    {
        // Normalize phone to E.164
        var telephoneId = TelephoneId.Create(request.Telephone);

        var exists = await _utilisateurRepository.ExistsByTelephoneAsync(
            telephoneId.Value, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Un utilisateur avec ce numéro de téléphone existe déjà.");

        var hash = _passwordHasher.Hash(request.MotDePasse);

        var utilisateur = Utilisateur.Create(
            telephoneId.Value,
            request.Nom,
            hash);

        await _utilisateurRepository.AddAsync(utilisateur, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(utilisateur);
        var refreshToken = await _refreshTokenService.GenerateAndStoreAsync(
            utilisateur.Id.Value, cancellationToken);

        _logger.LogInformation(
            "User registered successfully with ID {UserId}",
            utilisateur.Id.Value);

        return new AuthResult(
            utilisateur.Id.Value,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddHours(24));
    }
}
