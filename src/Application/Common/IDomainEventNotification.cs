namespace Application.Common;

using MediatR;

public interface IDomainEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEventNotification
{
}

public interface IDomainEventNotification : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
