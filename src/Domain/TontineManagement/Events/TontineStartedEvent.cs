namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class TontineStartedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public DateTime OccurredOn { get; }

    public TontineStartedEvent(TontineId tontineId)
    {
        TontineId = tontineId;
        OccurredOn = DateTime.UtcNow;
    }
}
