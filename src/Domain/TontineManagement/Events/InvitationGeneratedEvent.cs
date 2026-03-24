namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class InvitationGeneratedEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public InvitationId InvitationId { get; }
    public string Code { get; }
    public DateTime OccurredOn { get; }

    public InvitationGeneratedEvent(TontineId tontineId, InvitationId invitationId, string code)
    {
        TontineId = tontineId;
        InvitationId = invitationId;
        Code = code;
        OccurredOn = DateTime.UtcNow;
    }
}
