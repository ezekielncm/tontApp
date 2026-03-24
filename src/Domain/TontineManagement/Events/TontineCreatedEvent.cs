namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class TontineCreatedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public string Name { get; }
    public DateTime OccurredOn { get; }

    public TontineCreatedEvent(TontineId tontineId, string name)
    {
        TontineId = tontineId;
        Name = name;
        OccurredOn = DateTime.UtcNow;
    }
}
