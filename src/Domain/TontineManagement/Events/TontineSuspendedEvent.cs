namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class TontineSuspendedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public DateTime OccurredOn { get; }

    public TontineSuspendedEvent(TontineId tontineId)
    {
        TontineId = tontineId;
        OccurredOn = DateTime.UtcNow;
    }
}
