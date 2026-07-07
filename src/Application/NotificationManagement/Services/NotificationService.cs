namespace Application.NotificationManagement.Services;

using Domain.Common;
using Domain.NotificationManagement;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for creating notification outbox messages.
/// NEVER sends SMS directly — always goes through the Outbox pattern.
/// Enforces rate limiting (max 10 SMS/member/day) and opt-out rules.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification and adds it to the outbox for async processing.
    /// </summary>
    Task<bool> PlanifierNotificationAsync(
        string destinataireId,
        NotificationType type,
        string contenu,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates notifications in batch and adds them to the outbox for async processing.
    /// Returns the number of notifications successfully planned (bypassing rate limit).
    /// </summary>
    Task<int> PlanifierNotificationsAsync(
        IEnumerable<string> destinataireIds,
        NotificationType type,
        string contenu,
        CancellationToken cancellationToken = default);
}

public sealed class NotificationService : INotificationService
{
    private const int MaxSmsParJourParMembre = 10;

    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> PlanifierNotificationAsync(
        string destinataireId,
        NotificationType type,
        string contenu,
        CancellationToken cancellationToken = default)
    {
        // Critical notifications (payment confirmations) bypass rate limits
        var notification = Notification.CreateFull(
            destinataireId,
            Canal.SMS,
            type,
            contenu);

        if (!notification.EstCritique())
        {
            // Rate limit: max 10 SMS/member/day (non-critical only)
            var countToday = await _notificationRepository.CountTodayByDestinataireAsync(
                destinataireId, cancellationToken);

            if (countToday >= MaxSmsParJourParMembre)
            {
                _logger.LogWarning(
                    "Rate limit reached for member {DestinataireId}: {Count} SMS sent today (max {Max}). Notification dropped.",
                    destinataireId, countToday, MaxSmsParJourParMembre);
                return false;
            }
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} planned for {DestinataireId} (type: {Type})",
            notification.Id, destinataireId, type);

        return true;
    }

    public async Task<int> PlanifierNotificationsAsync(
        IEnumerable<string> destinataireIds,
        NotificationType type,
        string contenu,
        CancellationToken cancellationToken = default)
    {
        var idsList = destinataireIds.Distinct().ToList();
        if (!idsList.Any()) return 0;

        var notificationsToPlan = new List<Notification>();

        // We can create a dummy notification just to check if it's critical
        var dummy = Notification.CreateFull(
            "+1234567890", // dummy valid number
            Canal.SMS,
            type,
            contenu);

        bool isCritical = dummy.EstCritique();

        if (isCritical)
        {
            // If critical, no rate limits apply
            foreach (var id in idsList)
            {
                notificationsToPlan.Add(Notification.CreateFull(id, Canal.SMS, type, contenu));
            }
        }
        else
        {
            // Batch fetch rate limits for non-critical notifications
            var counts = await _notificationRepository.CountTodayByDestinatairesAsync(idsList, cancellationToken);

            foreach (var id in idsList)
            {
                int countToday = counts.ContainsKey(id) ? counts[id] : 0;
                if (countToday >= MaxSmsParJourParMembre)
                {
                    _logger.LogWarning(
                        "Rate limit reached for member {DestinataireId}: {Count} SMS sent today (max {Max}). Notification dropped.",
                        id, countToday, MaxSmsParJourParMembre);
                }
                else
                {
                    notificationsToPlan.Add(Notification.CreateFull(id, Canal.SMS, type, contenu));
                }
            }
        }

        if (!notificationsToPlan.Any())
            return 0;

        await _notificationRepository.AddRangeAsync(notificationsToPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Count} notifications planned in batch (type: {Type})",
            notificationsToPlan.Count, type);

        return notificationsToPlan.Count;
    }
}
