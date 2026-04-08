namespace Application.NotificationManagement.EventHandlers;

using Domain.NotificationManagement.ValueObjects;
using Domain.PaymentManagement.Events;
using Domain.TontineManagement.Repositories;
using Application.NotificationManagement.Services;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles VersementConfirmedEvent → SMS reçu de confirmation de paiement via Outbox.
/// JAMAIS d'appel SMS direct depuis ce handler.
/// Confirmation de paiement = toujours envoyée (même si opt-out).
/// </summary>
public sealed class VersementConfirmeEventHandler : INotificationHandler<VersementConfirmedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ITontineRepository _tontineRepository;
    private readonly ILogger<VersementConfirmeEventHandler> _logger;

    public VersementConfirmeEventHandler(
        INotificationService notificationService,
        ITontineRepository tontineRepository,
        ILogger<VersementConfirmeEventHandler> logger)
    {
        _notificationService = notificationService;
        _tontineRepository = tontineRepository;
        _logger = logger;
    }

    public async Task Handle(VersementConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling VersementConfirme for versement {VersementId}, payeur {PayeurId}, amount {Montant}",
            notification.VersementId, notification.PayeurId, notification.Montant);

        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(notification.TontineId, cancellationToken);
        var nomTontine = tontine?.Name ?? "votre tontine";

        var message = SmsTemplate.VersementConfirme(notification.Montant, "XOF", nomTontine);

        await _notificationService.PlanifierNotificationAsync(
            notification.PayeurId.Value.ToString(),
            NotificationType.ConfirmationPaiement,
            message,
            cancellationToken);
    }
}
