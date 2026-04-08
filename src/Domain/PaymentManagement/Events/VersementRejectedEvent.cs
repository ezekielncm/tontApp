namespace Domain.PaymentManagement.Events;

using Domain.Common;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class VersementRejectedEvent : IDomainEvent
{
    public VersementId VersementId { get; }
    public TontineId TontineId { get; }
    public PayeurId PayeurId { get; }
    public string Raison { get; }
    public DateTime OccurredOn { get; }

    public VersementRejectedEvent(VersementId versementId, TontineId tontineId, PayeurId payeurId, string raison)
    {
        VersementId = versementId;
        TontineId = tontineId;
        PayeurId = payeurId;
        Raison = raison;
        OccurredOn = DateTime.UtcNow;
    }
}
