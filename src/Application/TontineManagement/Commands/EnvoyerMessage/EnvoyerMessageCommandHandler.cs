namespace Application.TontineManagement.Commands.EnvoyerMessage;

using Application.Common;
using Application.NotificationManagement.Services;
using Domain.NotificationManagement.ValueObjects;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class EnvoyerMessageCommandHandler : ICommandHandler<EnvoyerMessageCommand, Guid>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly INotificationService _notificationService;

    public EnvoyerMessageCommandHandler(
        ITontineRepository tontineRepository,
        INotificationService notificationService)
    {
        _tontineRepository = tontineRepository;
        _notificationService = notificationService;
    }

    public async Task<Guid> Handle(EnvoyerMessageCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        if (tontine.GestionnaireId.Value != request.GestionnaireId)
            throw new InvalidOperationException("Only the gestionnaire of this tontine can send messages.");

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 300)
            throw new InvalidOperationException("Le message doit contenir entre 1 et 300 caractères.");

        var activeMembers = tontine.Members
            .Where(m => m.Statut == StatutMembre.Actif)
            .ToList();

        foreach (var member in activeMembers)
        {
            var destinataireId = member.UtilisateurId?.Value.ToString() ?? member.Id.Value.ToString();
            await _notificationService.PlanifierNotificationAsync(
                destinataireId,
                NotificationType.MessagePersonnalise,
                request.Message,
                cancellationToken);
        }

        return request.TontineId;
    }
}
