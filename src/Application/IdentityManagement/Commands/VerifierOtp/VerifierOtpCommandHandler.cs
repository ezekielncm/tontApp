namespace Application.IdentityManagement.Commands.VerifierOtp;

using Application.Common;
using Application.IdentityManagement.DTOs;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class VerifierOtpCommandHandler
    : ICommandHandler<VerifierOtpCommand, AuthResult>
{
    private readonly IOtpService _otpService;
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<VerifierOtpCommandHandler> _logger;

    public VerifierOtpCommandHandler(
        IOtpService otpService,
        IUtilisateurRepository utilisateurRepository,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILogger<VerifierOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _utilisateurRepository = utilisateurRepository;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(
        VerifierOtpCommand request,
        CancellationToken cancellationToken)
    {
        var telephone = TelephoneId.Create(request.Telephone).Value;

        var valid = await _otpService.ValidateAsync(telephone, request.Code, cancellationToken);
        if (!valid)
            throw new InvalidOperationException("Code OTP invalide ou expiré.");

        var utilisateur = await _utilisateurRepository.GetByTelephoneAsync(
            telephone, cancellationToken)
            ?? throw new InvalidOperationException("Utilisateur non trouvé.");

        var accessToken = _jwtService.GenerateAccessToken(utilisateur);
        var refreshToken = await _refreshTokenService.GenerateAndStoreAsync(
            utilisateur.Id.Value, cancellationToken);

        _logger.LogInformation("OTP verified for user {UserId}", utilisateur.Id.Value);

        return new AuthResult(
            utilisateur.Id.Value,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15));
    }
}
