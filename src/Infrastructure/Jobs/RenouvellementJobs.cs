namespace Infrastructure.Jobs;

using Application.NotificationManagement.Services;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;
using Domain.NotificationManagement.ValueObjects;
using Domain.PaymentManagement.Ports;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily Hangfire job: sends renewal reminder SMS 3 days before subscription expiration.
/// Runs daily at 08:00 UTC.
/// </summary>
public sealed class RappelRenouvellementJ3Job
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IPlanAbonnementRepository _planRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RappelRenouvellementJ3Job> _logger;

    public RappelRenouvellementJ3Job(
        IAbonnementRepository abonnementRepository,
        IPlanAbonnementRepository planRepository,
        INotificationService notificationService,
        ILogger<RappelRenouvellementJ3Job> logger)
    {
        _abonnementRepository = abonnementRepository;
        _planRepository = planRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RappelRenouvellementJ3Job: checking for subscriptions expiring in 3 days...");

        var targetDate = DateTime.UtcNow.Date.AddDays(3);
        var expiringAbonnements = await _abonnementRepository.GetExpiringAsync(targetDate, cancellationToken);

        var notificationsSent = 0;

        foreach (var abonnement in expiringAbonnements)
        {
            if (abonnement.DateFin.Date != targetDate)
                continue;

            var plan = await _planRepository.GetByIdAsync(abonnement.PlanId, cancellationToken);
            var planNom = plan?.Nom ?? abonnement.Plan.ToString();

            var message = SmsTemplate.RappelRenouvellement(
                planNom, abonnement.MontantMensuel, abonnement.Currency, abonnement.DateFin);

            await _notificationService.PlanifierNotificationAsync(
                abonnement.GestionnaireId,
                NotificationType.RappelRenouvellement,
                message,
                cancellationToken);

            notificationsSent++;
        }

        _logger.LogInformation(
            "RappelRenouvellementJ3Job: {Count} renewal reminders sent", notificationsSent);
    }
}

/// <summary>
/// Daily Hangfire job: attempts automatic renewal on subscription expiration day (J0).
/// If debit fails, subscription enters 3-day grace period.
/// If grace period has expired, subscription is expired (soft downgrade to free).
/// Runs daily at 00:30 UTC.
/// </summary>
public sealed class RenouvellementAbonnementJob
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IPlanAbonnementRepository _planRepository;
    private readonly IMobileMoneyGateway _mobileMoneyGateway;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RenouvellementAbonnementJob> _logger;

    public RenouvellementAbonnementJob(
        IAbonnementRepository abonnementRepository,
        IPlanAbonnementRepository planRepository,
        IMobileMoneyGateway mobileMoneyGateway,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<RenouvellementAbonnementJob> logger)
    {
        _abonnementRepository = abonnementRepository;
        _planRepository = planRepository;
        _mobileMoneyGateway = mobileMoneyGateway;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RenouvellementAbonnementJob: processing renewals...");

        await ProcessExpiringAbonnementsAsync(cancellationToken);
        await ProcessGraceAbonnementsAsync(cancellationToken);
    }

    private async Task ProcessExpiringAbonnementsAsync(CancellationToken cancellationToken)
    {
        // Get subscriptions expiring today or earlier (with auto-renewal)
        var now = DateTime.UtcNow;
        var expiringAbonnements = await _abonnementRepository.GetExpiringAsync(now, cancellationToken);

        _logger.LogInformation(
            "RenouvellementAbonnementJob: found {Count} expiring subscriptions", expiringAbonnements.Count);

        foreach (var abonnement in expiringAbonnements)
        {
            if (abonnement.Plan == PlanTarifaire.Gratuit)
                continue; // Free plan never expires

            try
            {
                await AttemptRenewalAsync(abonnement, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RenouvellementAbonnementJob: unexpected error processing abonnement {AbonnementId}",
                    abonnement.Id.Value);
            }
        }
    }

    private async Task AttemptRenewalAsync(Abonnement abonnement, CancellationToken cancellationToken)
    {
        var reference = $"RENEW-{abonnement.Id.Value:N}-{DateTime.UtcNow:yyyyMMdd}";

        // Attempt Orange Money debit
        var paymentRequest = new MobileMoneyRequest(
            abonnement.GestionnaireId, // In real impl, would need phone from user profile
            abonnement.MontantMensuel,
            abonnement.Currency,
            reference);

        var paymentResponse = await _mobileMoneyGateway.InitierPaiementAsync(
            paymentRequest, cancellationToken);

        if (paymentResponse.Success)
        {
            // Renewal successful
            abonnement.Renouveler(paymentResponse.TransactionId);
            await _abonnementRepository.UpdateAsync(abonnement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var plan = await _planRepository.GetByIdAsync(abonnement.PlanId, cancellationToken);
            var planNom = plan?.Nom ?? abonnement.Plan.ToString();

            var message = SmsTemplate.RenouvellementReussi(planNom, abonnement.DateFin);
            await _notificationService.PlanifierNotificationAsync(
                abonnement.GestionnaireId,
                NotificationType.RenouvellementReussi,
                message,
                cancellationToken);

            _logger.LogInformation(
                "RenouvellementAbonnementJob: renewed abonnement {AbonnementId} until {DateFin}",
                abonnement.Id.Value, abonnement.DateFin);
        }
        else
        {
            // Payment failed: enter grace period
            abonnement.PasserEnGrace();
            await _abonnementRepository.UpdateAsync(abonnement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var plan = await _planRepository.GetByIdAsync(abonnement.PlanId, cancellationToken);
            var planNom = plan?.Nom ?? abonnement.Plan.ToString();

            var message = SmsTemplate.RenouvellementEchoue(planNom);
            await _notificationService.PlanifierNotificationAsync(
                abonnement.GestionnaireId,
                NotificationType.RenouvellementEchoue,
                message,
                cancellationToken);

            _logger.LogWarning(
                "RenouvellementAbonnementJob: payment failed for abonnement {AbonnementId}, entered grace period until {DateFinGrace}",
                abonnement.Id.Value, abonnement.DateFinGrace);
        }
    }

    private async Task ProcessGraceAbonnementsAsync(CancellationToken cancellationToken)
    {
        // Get subscriptions whose grace period has expired
        var graceAbonnements = await _abonnementRepository.GetInGraceAsync(cancellationToken);

        _logger.LogInformation(
            "RenouvellementAbonnementJob: found {Count} subscriptions with expired grace period",
            graceAbonnements.Count);

        foreach (var abonnement in graceAbonnements)
        {
            try
            {
                // Soft downgrade: expire the subscription
                abonnement.Expirer();
                await _abonnementRepository.UpdateAsync(abonnement, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "RenouvellementAbonnementJob: expired abonnement {AbonnementId} after grace period",
                    abonnement.Id.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RenouvellementAbonnementJob: error expiring abonnement {AbonnementId}",
                    abonnement.Id.Value);
            }
        }
    }
}
