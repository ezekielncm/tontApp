namespace Domain.BillingManagement.Events;

using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class AbonnementRenouvelleEvent : IDomainEvent
{
    public AbonnementId AbonnementId { get; }
    public string GestionnaireId { get; }
    public PlanTarifaire Plan { get; }
    public DateTime NouvelleDateFin { get; }
    public DateTime OccurredOn { get; }

    public AbonnementRenouvelleEvent(
        AbonnementId abonnementId,
        string gestionnaireId,
        PlanTarifaire plan,
        DateTime nouvelleDateFin)
    {
        AbonnementId = abonnementId;
        GestionnaireId = gestionnaireId;
        Plan = plan;
        NouvelleDateFin = nouvelleDateFin;
        OccurredOn = DateTime.UtcNow;
    }
}
