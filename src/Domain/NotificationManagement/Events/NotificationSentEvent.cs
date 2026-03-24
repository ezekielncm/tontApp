namespace Domain.NotificationManagement.Events;

using Domain.Common;
using Domain.NotificationManagement.ValueObjects;

public sealed class NotificationSentEvent : IDomainEvent
{
    public NotificationId NotificationId { get; }
    public DateTime OccurredOn { get; }

    public NotificationSentEvent(NotificationId notificationId)
    {
        NotificationId = notificationId;
        OccurredOn = DateTime.UtcNow;
    }
}
