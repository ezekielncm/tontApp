namespace Application.NotificationManagement.Queries.GetPendingNotifications;

using Application.Common;
using Domain.NotificationManagement.Repositories;

public sealed class GetPendingNotificationsQueryHandler
    : IQueryHandler<GetPendingNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetPendingNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetPendingNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetPendingAsync(cancellationToken);

        return notifications.Select(n => new NotificationDto(
            n.Id.Value,
            n.DestinataireId,
            n.Type.ToString(),
            n.Contenu,
            n.Statut.ToString(),
            n.TentativesEnvoi,
            n.MaxTentatives,
            n.CreatedAt,
            n.SentAt)).ToList();
    }
}
