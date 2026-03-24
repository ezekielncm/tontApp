namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class TontineActivatedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public DateTime OccurredOn { get; }

    public TontineActivatedEvent(TontineId tontineId)
    {
        TontineId = tontineId;
        OccurredOn = DateTime.UtcNow;
    }
}
