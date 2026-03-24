namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class RoundOpenedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public RoundId RoundId { get; }
    public MemberId BeneficiaryId { get; }
    public int RoundNumber { get; }
    public DateTime OccurredOn { get; }

    public RoundOpenedEvent(TontineId tontineId, RoundId roundId, MemberId beneficiaryId, int roundNumber)
    {
        TontineId = tontineId;
        RoundId = roundId;
        BeneficiaryId = beneficiaryId;
        RoundNumber = roundNumber;
        OccurredOn = DateTime.UtcNow;
    }
}
