namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using Application.Common;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Domain.NotificationManagement.Ports;
using Microsoft.Extensions.Logging;

public sealed class InscrireUtilisateurCommandHandler
    : ICommandHandler<InscrireUtilisateurCommand, InscrireUtilisateurResult>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpService _otpService;
    private readonly ISmsGateway _smsGateway;
    private readonly ILogger<InscrireUtilisateurCommandHandler> _logger;

    public InscrireUtilisateurCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IOtpService otpService,
        ISmsGateway smsGateway,
        ILogger<InscrireUtilisateurCommandHandler> logger)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
        _smsGateway = smsGateway;
        _logger = logger;
    }

    public async Task<InscrireUtilisateurResult> Handle(
        InscrireUtilisateurCommand request,
        CancellationToken cancellationToken)
    {
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

        // Generate OTP and send via SMS
        var otp = await _otpService.GenerateAndStoreAsync(telephoneId.Value, cancellationToken);
        await _smsGateway.EnvoyerAsync(
            telephoneId.Value,
            $"Votre code de vérification TontinesApp : {otp}. Valide 5 minutes.",
            cancellationToken);

        _logger.LogInformation(
            "User registered with ID {UserId}, OTP sent to {Telephone}",
            utilisateur.Id.Value, telephoneId.Value);

        return new InscrireUtilisateurResult(
            utilisateur.Id.Value,
            "Inscription réussie. Un code OTP a été envoyé par SMS.");
    }
}
