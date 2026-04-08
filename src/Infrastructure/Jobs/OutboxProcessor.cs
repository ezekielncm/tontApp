namespace Infrastructure.Jobs;

using System.Text.Json;
using Domain.Common;
using Domain.NotificationManagement;
using Domain.NotificationManagement.Ports;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hangfire job that processes outbox messages every 30 seconds.
/// Picks up pending notifications and sends them via ISmsGateway.
/// Guarantees no notification is lost even if the process crashes.
/// </summary>
public sealed class OutboxProcessor
{
    private const int BatchSize = 50;

    private readonly TontineDbContext _dbContext;
    private readonly ISmsGateway _smsGateway;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        TontineDbContext dbContext,
        ISmsGateway smsGateway,
        INotificationRepository notificationRepository,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _smsGateway = smsGateway;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processes all unprocessed outbox messages and sends pending notifications.
    /// Called by Hangfire every 30 seconds.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OutboxProcessor: checking for pending messages...");

        // 1. Process generic outbox messages
        await ProcessOutboxMessagesAsync(cancellationToken);

        // 2. Process pending notifications
        await ProcessPendingNotificationsAsync(cancellationToken);
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        _logger.LogInformation("OutboxProcessor: processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Mark as processed - the notification was already created in the same transaction
                message.MarkProcessed();

                _logger.LogDebug("OutboxProcessor: processed outbox message {MessageId} ({Type})",
                    message.Id, message.TypeEvenement);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex,
                    "OutboxProcessor: failed to process outbox message {MessageId}",
                    message.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        var pendingNotifications = await _notificationRepository.GetPendingAsync(cancellationToken);

        if (pendingNotifications.Count == 0)
            return;

        _logger.LogInformation("OutboxProcessor: sending {Count} pending notifications", pendingNotifications.Count);

        foreach (var notification in pendingNotifications)
        {
            if (!notification.PeutReessayer())
                continue;

            // Only send if dateEnvoi is null (immediate) or if scheduled time has passed
            if (notification.DateEnvoi.HasValue && notification.DateEnvoi.Value > DateTime.UtcNow)
                continue;

            try
            {
                if (notification.Canal == Canal.SMS)
                {
                    var result = await _smsGateway.EnvoyerAsync(
                        notification.DestinataireId,
                        notification.Contenu,
                        cancellationToken);

                    if (result.Success)
                    {
                        notification.MarquerEnvoyee();
                        _logger.LogInformation(
                            "SMS sent for notification {NotificationId} to {Destinataire}, messageId={MessageId}",
                            notification.Id, notification.DestinataireId, result.MessageId);
                    }
                    else
                    {
                        var canRetry = notification.MarquerEchouee();
                        _logger.LogWarning(
                            "SMS failed for notification {NotificationId} to {Destinataire}: {Description}. CanRetry={CanRetry}",
                            notification.Id, notification.DestinataireId, result.Description, canRetry);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Unsupported canal {Canal} for notification {NotificationId}. Skipping.",
                        notification.Canal, notification.Id);
                }

                await _notificationRepository.UpdateAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                notification.MarquerEchouee();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);

                _logger.LogError(ex,
                    "Exception while sending notification {NotificationId}",
                    notification.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
