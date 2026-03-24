namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class MemberRemovedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public MemberId MemberId { get; }
    public DateTime OccurredOn { get; }

    public MemberRemovedEvent(TontineId tontineId, MemberId memberId)
    {
        TontineId = tontineId;
        MemberId = memberId;
        OccurredOn = DateTime.UtcNow;
    }
}
