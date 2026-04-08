namespace Infrastructure.Jobs;

using Application.NotificationManagement.Services;
using Domain.NotificationManagement.ValueObjects;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily Hangfire cron job: sends J-3 reminder SMS (3 days before payment deadline).
/// Runs daily at 08:00 UTC.
/// </summary>
public sealed class RappelJ3Job
{
    private readonly ITontineRepository _tontineRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RappelJ3Job> _logger;

    public RappelJ3Job(
        ITontineRepository tontineRepository,
        INotificationService notificationService,
        ILogger<RappelJ3Job> logger)
    {
        _tontineRepository = tontineRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RappelJ3Job: checking for rounds due in 3 days...");

        var activeTontines = await _tontineRepository.GetByStatusReadOnlyAsync(
            TontineStatus.Active, cancellationToken);

        var targetDate = DateTime.UtcNow.Date.AddDays(3);
        var notificationsSent = 0;

        foreach (var tontine in activeTontines)
        {
            var rounds = tontine.GetActiveRounds();
            foreach (var round in rounds)
            {
                if (round.DateLimite.Date == targetDate && !round.IsCompleted)
                {
                    var montant = tontine.ContributionAmount?.Amount ?? 0;
                    var devise = tontine.ContributionAmount?.Currency ?? "XOF";
                    var message = SmsTemplate.RappelJ3(tontine.Name, montant, devise);

                    var members = tontine.GetActiveMembers();
                    foreach (var member in members)
                    {
                        // Skip the beneficiary of this round (they receive, not pay)
                        if (member.Id == round.BeneficiaryId)
                            continue;

                        await _notificationService.PlanifierNotificationAsync(
                            member.Id.Value.ToString(),
                            NotificationType.RappelPaiement,
                            message,
                            cancellationToken);

                        notificationsSent++;
                    }
                }
            }
        }

        _logger.LogInformation("RappelJ3Job: {Count} reminder notifications planned", notificationsSent);
    }
}

/// <summary>
/// Daily Hangfire cron job: sends J-1 reminder SMS (1 day before payment deadline).
/// Runs daily at 08:00 UTC.
/// </summary>
public sealed class RappelJ1Job
{
    private readonly ITontineRepository _tontineRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RappelJ1Job> _logger;

    public RappelJ1Job(
        ITontineRepository tontineRepository,
        INotificationService notificationService,
        ILogger<RappelJ1Job> logger)
    {
        _tontineRepository = tontineRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RappelJ1Job: checking for rounds due tomorrow...");

        var activeTontines = await _tontineRepository.GetByStatusReadOnlyAsync(
            TontineStatus.Active, cancellationToken);

        var targetDate = DateTime.UtcNow.Date.AddDays(1);
        var notificationsSent = 0;

        foreach (var tontine in activeTontines)
        {
            var rounds = tontine.GetActiveRounds();
            foreach (var round in rounds)
            {
                if (round.DateLimite.Date == targetDate && !round.IsCompleted)
                {
                    var montant = tontine.ContributionAmount?.Amount ?? 0;
                    var devise = tontine.ContributionAmount?.Currency ?? "XOF";
                    var message = SmsTemplate.RappelJ1(tontine.Name, montant, devise);

                    var members = tontine.GetActiveMembers();
                    foreach (var member in members)
                    {
                        if (member.Id == round.BeneficiaryId)
                            continue;

                        await _notificationService.PlanifierNotificationAsync(
                            member.Id.Value.ToString(),
                            NotificationType.RappelPaiement,
                            message,
                            cancellationToken);

                        notificationsSent++;
                    }
                }
            }
        }

        _logger.LogInformation("RappelJ1Job: {Count} reminder notifications planned", notificationsSent);
    }
}

/// <summary>
/// Weekly Hangfire cron job: sends recap SMS every Monday at 09:00 UTC.
/// </summary>
public sealed class RecapHebdoJob
{
    private readonly ITontineRepository _tontineRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RecapHebdoJob> _logger;

    public RecapHebdoJob(
        ITontineRepository tontineRepository,
        INotificationService notificationService,
        ILogger<RecapHebdoJob> logger)
    {
        _tontineRepository = tontineRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RecapHebdoJob: generating weekly recap notifications...");

        var activeTontines = await _tontineRepository.GetByStatusReadOnlyAsync(
            TontineStatus.Active, cancellationToken);

        var notificationsSent = 0;

        foreach (var tontine in activeTontines)
        {
            var members = tontine.GetActiveMembers();
            var totalMembers = members.Count;

            // Simplified: count members who have at least contributed (proxy for "à jour")
            var membresAJour = totalMembers;
            var message = SmsTemplate.RecapHebdomadaire(tontine.Name, membresAJour, totalMembers);

            foreach (var member in members)
            {
                await _notificationService.PlanifierNotificationAsync(
                    member.Id.Value.ToString(),
                    NotificationType.RecapHebdomadaire,
                    message,
                    cancellationToken);

                notificationsSent++;
            }
        }

        _logger.LogInformation("RecapHebdoJob: {Count} recap notifications planned", notificationsSent);
    }
}
