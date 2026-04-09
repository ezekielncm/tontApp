namespace Application.NotificationManagement.EventHandlers;

using Domain.NotificationManagement.ValueObjects;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.Repositories;
using Application.NotificationManagement.Services;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles MembreSuspenduEvent → SMS notification au membre suspendu via Outbox.
/// JAMAIS d'appel SMS direct depuis ce handler.
/// </summary>
public sealed class MembreSuspenduEventHandler : INotificationHandler<MembreSuspenduEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ITontineRepository _tontineRepository;
    private readonly ILogger<MembreSuspenduEventHandler> _logger;

    public MembreSuspenduEventHandler(
        INotificationService notificationService,
        ITontineRepository tontineRepository,
        ILogger<MembreSuspenduEventHandler> logger)
    {
        _notificationService = notificationService;
        _tontineRepository = tontineRepository;
        _logger = logger;
    }

    public async Task Handle(MembreSuspenduEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling MembreSuspendu for tontine {TontineId}, membre {MembreId}, motif: {Motif}",
            notification.TontineId, notification.MembreId, notification.Motif);

        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(notification.TontineId, cancellationToken);
        var nomTontine = tontine?.Name ?? "votre tontine";

        var message = SmsTemplate.MembreSuspendu(nomTontine, notification.Motif);

        await _notificationService.PlanifierNotificationAsync(
            notification.MembreId.Value.ToString(),
            NotificationType.Suspension,
            message,
            cancellationToken);
    }
}
