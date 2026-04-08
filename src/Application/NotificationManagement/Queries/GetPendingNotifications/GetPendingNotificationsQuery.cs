namespace Application.NotificationManagement.Queries.GetPendingNotifications;

using Application.Common;

public sealed record GetPendingNotificationsQuery : IQuery<IReadOnlyList<NotificationDto>>;
