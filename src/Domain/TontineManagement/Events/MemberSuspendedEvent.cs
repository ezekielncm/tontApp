namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class MemberSuspendedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public MemberId MemberId { get; }
    public DateTime OccurredOn { get; }

    public MemberSuspendedEvent(TontineId tontineId, MemberId memberId)
    {
        TontineId = tontineId;
        MemberId = memberId;
        OccurredOn = DateTime.UtcNow;
    }
}
