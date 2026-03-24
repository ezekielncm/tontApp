namespace Domain.NotificationManagement.Events;

using Domain.Common;
using Domain.NotificationManagement.ValueObjects;

public sealed class NotificationCreatedEvent : IDomainEvent
{
    public NotificationId NotificationId { get; }
    public string DestinataireId { get; }
    public NotificationType Type { get; }
    public DateTime OccurredOn { get; }

    public NotificationCreatedEvent(NotificationId notificationId, string destinataireId, NotificationType type)
    {
        NotificationId = notificationId;
        DestinataireId = destinataireId;
        Type = type;
        OccurredOn = DateTime.UtcNow;
    }
}
