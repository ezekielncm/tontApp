namespace Domain.PaymentManagement.Events;

using Domain.Common;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class VersementConfirmedEvent : IDomainEvent
{
    public VersementId VersementId { get; }
    public TontineId TontineId { get; }
    public MemberId MemberId { get; }
    public RoundId RoundId { get; }
    public decimal Montant { get; }
    public string ReferenceExterne { get; }
    public DateTime OccurredOn { get; }

    public VersementConfirmedEvent(
        VersementId versementId,
        TontineId tontineId,
        MemberId memberId,
        RoundId roundId,
        decimal montant,
        string referenceExterne)
    {
        VersementId = versementId;
        TontineId = tontineId;
        MemberId = memberId;
        RoundId = roundId;
        Montant = montant;
        ReferenceExterne = referenceExterne;
        OccurredOn = DateTime.UtcNow;
    }
}
