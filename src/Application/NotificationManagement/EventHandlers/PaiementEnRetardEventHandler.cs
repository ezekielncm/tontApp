namespace Application.NotificationManagement.EventHandlers;

using Domain.NotificationManagement.ValueObjects;
using Domain.PaymentManagement.Events;
using Domain.TontineManagement.Repositories;
using Application.NotificationManagement.Services;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles PaiementEnRetardEvent → SMS rappel de retard de paiement via Outbox.
/// JAMAIS d'appel SMS direct depuis ce handler.
/// </summary>
public sealed class PaiementEnRetardEventHandler : INotificationHandler<PaiementEnRetardEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ITontineRepository _tontineRepository;
    private readonly ILogger<PaiementEnRetardEventHandler> _logger;

    public PaiementEnRetardEventHandler(
        INotificationService notificationService,
        ITontineRepository tontineRepository,
        ILogger<PaiementEnRetardEventHandler> logger)
    {
        _notificationService = notificationService;
        _tontineRepository = tontineRepository;
        _logger = logger;
    }

    public async Task Handle(PaiementEnRetardEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling PaiementEnRetard for tontine {TontineId}, payeur {PayeurId}, amount {Montant}",
            notification.TontineId, notification.PayeurId, notification.Montant);

        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(notification.TontineId, cancellationToken);
        var nomTontine = tontine?.Name ?? "votre tontine";

        // Notifier le membre en retard
        var message = SmsTemplate.PaiementEnRetard(nomTontine, notification.Montant, notification.Devise);

        await _notificationService.PlanifierNotificationAsync(
            notification.PayeurId.Value.ToString(),
            NotificationType.RappelPaiement,
            message,
            cancellationToken);

        // Notifier le gestionnaire de la tontine
        if (tontine?.GestionnaireId is not null)
        {
            var messageGestionnaire = SmsTemplate.RetardPourGestionnaire(
                nomTontine, notification.Montant, notification.Devise);

            await _notificationService.PlanifierNotificationAsync(
                tontine.GestionnaireId.Value.ToString(),
                NotificationType.RappelPaiement,
                messageGestionnaire,
                cancellationToken);
        }
    }
}
