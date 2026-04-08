namespace Application.NotificationManagement.EventHandlers;

using Domain.NotificationManagement.ValueObjects;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.Repositories;
using Application.NotificationManagement.Services;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles TourOuvert (RoundOpenedEvent) → notifie le bénéficiaire du tour via Outbox.
/// JAMAIS d'appel SMS direct depuis ce handler.
/// </summary>
public sealed class TourOuvertEventHandler : INotificationHandler<RoundOpenedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ITontineRepository _tontineRepository;
    private readonly ILogger<TourOuvertEventHandler> _logger;

    public TourOuvertEventHandler(
        INotificationService notificationService,
        ITontineRepository tontineRepository,
        ILogger<TourOuvertEventHandler> logger)
    {
        _notificationService = notificationService;
        _tontineRepository = tontineRepository;
        _logger = logger;
    }

    public async Task Handle(RoundOpenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling TourOuvert for tontine {TontineId}, round {RoundNumber}, beneficiary {BeneficiaryId}",
            notification.TontineId, notification.RoundNumber, notification.BeneficiaryId);

        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(notification.TontineId, cancellationToken);
        var nomTontine = tontine?.Name ?? "votre tontine";

        var message = SmsTemplate.TourOuvert(nomTontine, notification.RoundNumber);

        await _notificationService.PlanifierNotificationAsync(
            notification.BeneficiaryId.Value.ToString(),
            NotificationType.OuvertureTour,
            message,
            cancellationToken);
    }
}
