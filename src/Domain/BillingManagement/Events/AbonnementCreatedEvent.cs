namespace Domain.BillingManagement.Events;

using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class AbonnementCreatedEvent : IDomainEvent
{
    public AbonnementId AbonnementId { get; }
    public string GestionnaireId { get; }
    public PlanTarifaire Plan { get; }
    public DateTime OccurredOn { get; }

    public AbonnementCreatedEvent(AbonnementId abonnementId, string gestionnaireId, PlanTarifaire plan)
    {
        AbonnementId = abonnementId;
        GestionnaireId = gestionnaireId;
        Plan = plan;
        OccurredOn = DateTime.UtcNow;
    }
}
