namespace Application.NotificationManagement.Queries.GetNotificationStats;

using Application.Common;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;

public sealed class GetNotificationStatsQueryHandler
    : IQueryHandler<GetNotificationStatsQuery, NotificationStatsDto>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationStatsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationStatsDto> Handle(
        GetNotificationStatsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _notificationRepository.GetAllAsync(cancellationToken);

        var totalEnvoyees = all.Count(n => n.Statut == NotificationStatus.Envoyee);
        var totalEchouees = all.Count(n => n.Statut == NotificationStatus.Echouee);
        var totalEnAttente = all.Count(n => n.Statut == NotificationStatus.EnAttente);
        var tauxEchec = all.Count > 0
            ? Math.Round((double)totalEchouees / all.Count * 100, 1)
            : 0;

        var parType = all
            .GroupBy(n => n.Type)
            .Select(g => new NotificationTypeStatDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(t => t.Count)
            .ToList();

        return new NotificationStatsDto(
            totalEnvoyees,
            totalEchouees,
            totalEnAttente,
            tauxEchec,
            parType);
    }
}
