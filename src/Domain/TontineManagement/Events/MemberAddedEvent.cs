namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class MemberAddedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public MemberId MemberId { get; }
    public string MemberName { get; }
    public DateTime OccurredOn { get; }

    public MemberAddedEvent(TontineId tontineId, MemberId memberId, string memberName)
    {
        TontineId = tontineId;
        MemberId = memberId;
        MemberName = memberName;
        OccurredOn = DateTime.UtcNow;
    }
}
