namespace Application.BillingManagement.Commands.SouscrireAbonnement;

using Application.BillingManagement.Services;
using Application.Common;
using Application.NotificationManagement.Services;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;
using Domain.NotificationManagement.ValueObjects;
using Domain.PaymentManagement.Ports;

public sealed class SouscrireAbonnementCommandHandler
    : ICommandHandler<SouscrireAbonnementCommand, SouscrireAbonnementResult>
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IPlanAbonnementRepository _planRepository;
    private readonly IMobileMoneyGateway _mobileMoneyGateway;
    private readonly INotificationService _notificationService;
    private readonly IBillingCacheService _billingCache;
    private readonly IUnitOfWork _unitOfWork;

    public SouscrireAbonnementCommandHandler(
        IAbonnementRepository abonnementRepository,
        IPlanAbonnementRepository planRepository,
        IMobileMoneyGateway mobileMoneyGateway,
        INotificationService notificationService,
        IBillingCacheService billingCache,
        IUnitOfWork unitOfWork)
    {
        _abonnementRepository = abonnementRepository;
        _planRepository = planRepository;
        _mobileMoneyGateway = mobileMoneyGateway;
        _notificationService = notificationService;
        _billingCache = billingCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<SouscrireAbonnementResult> Handle(
        SouscrireAbonnementCommand request,
        CancellationToken cancellationToken)
    {
        // Check if gestionnaire already has an active subscription
        var existing = await _abonnementRepository.GetByGestionnaireAsync(
            request.GestionnaireId, cancellationToken);

        if (existing is not null && existing.EstFonctionnellementActif())
            throw new InvalidOperationException("Un abonnement actif existe déjà pour ce gestionnaire.");

        // Get the plan details
        var planCode = request.PlanCode.ToUpperInvariant();
        var plan = await _planRepository.GetByCodeAsync(planCode, cancellationToken)
            ?? throw new InvalidOperationException($"Plan '{planCode}' introuvable.");

        var planTarifaire = Enum.Parse<PlanTarifaire>(planCode, ignoreCase: true);

        string? transactionId = null;
        var paiementInitie = false;

        // For paid plans, initiate Orange Money payment
        if (plan.PrixMensuel > 0)
        {
            var paymentRequest = new MobileMoneyRequest(
                request.NumeroTelephone,
                plan.PrixMensuel,
                plan.Devise,
                $"ABO-{request.GestionnaireId[..Math.Min(8, request.GestionnaireId.Length)]}-{DateTime.UtcNow:yyyyMMddHHmmss}");

            var paymentResponse = await _mobileMoneyGateway.InitierPaiementAsync(
                paymentRequest, cancellationToken);

            if (!paymentResponse.Success)
                throw new InvalidOperationException(
                    $"Échec de l'initiation du paiement Orange Money: {paymentResponse.Description}");

            transactionId = paymentResponse.TransactionId;
            paiementInitie = true;
        }

        // Create or upgrade subscription
        var abonnement = Abonnement.CreateAvecPlan(
            request.GestionnaireId,
            plan.Id,
            planTarifaire,
            plan.PrixMensuel,
            transactionId);

        if (existing is not null)
        {
            // Replace the expired/cancelled subscription
            existing.Annuler();
            await _abonnementRepository.UpdateAsync(existing, cancellationToken);
        }

        await _abonnementRepository.AddAsync(abonnement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update Redis cache with new plan limits
        await _billingCache.SetPlanLimitsAsync(
            request.GestionnaireId,
            plan.MaxTontines,
            plan.MaxMembresParTontine,
            cancellationToken);

        // Send confirmation SMS
        var smsMessage = SmsTemplate.ConfirmationAbonnement(
            plan.Nom, plan.PrixMensuel, plan.Devise);

        await _notificationService.PlanifierNotificationAsync(
            request.GestionnaireId,
            NotificationType.ConfirmationAbonnement,
            smsMessage,
            cancellationToken);

        return new SouscrireAbonnementResult(
            abonnement.Id.Value,
            abonnement.Plan.ToString(),
            abonnement.Statut.ToString(),
            abonnement.MontantMensuel,
            abonnement.Currency,
            abonnement.DateDebut,
            abonnement.DateFin,
            paiementInitie,
            transactionId);
    }
}
