namespace Domain.PaymentManagement.Events;

using Domain.Common;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class VersementCreatedEvent : IDomainEvent
{
    public VersementId VersementId { get; }
    public TontineId TontineId { get; }
    public MemberId MemberId { get; }
    public decimal Montant { get; }
    public DateTime OccurredOn { get; }

    public VersementCreatedEvent(VersementId versementId, TontineId tontineId, MemberId memberId, decimal montant)
    {
        VersementId = versementId;
        TontineId = tontineId;
        MemberId = memberId;
        Montant = montant;
        OccurredOn = DateTime.UtcNow;
    }
}
