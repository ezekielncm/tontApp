namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class RoundClosedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public RoundId RoundId { get; }
    public int RoundNumber { get; }
    public DateTime OccurredOn { get; }

    public RoundClosedEvent(TontineId tontineId, RoundId roundId, int roundNumber)
    {
        TontineId = tontineId;
        RoundId = roundId;
        RoundNumber = roundNumber;
        OccurredOn = DateTime.UtcNow;
    }
}
