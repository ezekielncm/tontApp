namespace Domain.PaymentManagement.Events;

using Domain.Common;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

/// <summary>
/// Domain event raised when a payment is overdue for a tontine round.
/// </summary>
public sealed class PaiementEnRetardEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public TourId TourId { get; }
    public PayeurId PayeurId { get; }
    public decimal Montant { get; }
    public string Devise { get; }
    public DateTime OccurredOn { get; }

    public PaiementEnRetardEvent(
        TontineId tontineId,
        TourId tourId,
        PayeurId payeurId,
        decimal montant,
        string devise)
    {
        TontineId = tontineId;
        TourId = tourId;
        PayeurId = payeurId;
        Montant = montant;
        Devise = devise;
        OccurredOn = DateTime.UtcNow;
    }
}
