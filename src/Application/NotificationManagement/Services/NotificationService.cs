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
        // Rate limit: max 10 SMS/member/day
        var countToday = await _notificationRepository.CountTodayByDestinataireAsync(
            destinataireId, cancellationToken);

        if (countToday >= MaxSmsParJourParMembre)
        {
            _logger.LogWarning(
                "Rate limit reached for member {DestinataireId}: {Count} SMS sent today (max {Max}). Notification dropped.",
                destinataireId, countToday, MaxSmsParJourParMembre);
            return false;
        }

        var notification = Notification.CreateFull(
            destinataireId,
            Canal.SMS,
            type,
            contenu);

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} planned for {DestinataireId} (type: {Type})",
            notification.Id, destinataireId, type);

        return true;
    }
}
