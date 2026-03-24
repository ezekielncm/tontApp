namespace Domain.BillingManagement;

using Domain.BillingManagement.Events;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class Abonnement : AggregateRoot<AbonnementId>
{
    public string GestionnaireId { get; private set; }
    public PlanTarifaire Plan { get; private set; }
    public StatutAbonnement Statut { get; private set; }
    public decimal MontantMensuel { get; private set; }
    public string Currency { get; private set; }
    public DateTime DateDebut { get; private set; }
    public DateTime DateFin { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Abonnement(
        AbonnementId id,
        string gestionnaireId,
        PlanTarifaire plan,
        decimal montantMensuel,
        string currency,
        DateTime dateDebut,
        DateTime dateFin) : base(id)
    {
        GestionnaireId = gestionnaireId;
        Plan = plan;
        MontantMensuel = montantMensuel;
        Currency = currency;
        Statut = StatutAbonnement.Actif;
        DateDebut = dateDebut;
        DateFin = dateFin;
        CreatedAt = DateTime.UtcNow;
    }

    public static Abonnement Create(string gestionnaireId, PlanTarifaire plan)
    {
        if (string.IsNullOrWhiteSpace(gestionnaireId))
            throw new ArgumentException("GestionnaireId cannot be empty.", nameof(gestionnaireId));

        var montant = plan switch
        {
            PlanTarifaire.Gratuit => 0m,
            PlanTarifaire.Pro => 2000m,
            PlanTarifaire.Imf => 0m,
            _ => throw new ArgumentOutOfRangeException(nameof(plan))
        };

        var now = DateTime.UtcNow;
        var dateFin = plan == PlanTarifaire.Gratuit
            ? DateTime.MaxValue
            : now.AddMonths(1);

        var abonnement = new Abonnement(
            AbonnementId.Create(),
            gestionnaireId,
            plan,
            montant,
            "XOF",
            now,
            dateFin);

        abonnement.AddDomainEvent(new AbonnementCreatedEvent(abonnement.Id, gestionnaireId, plan));

        return abonnement;
    }

    public void Renouveler()
    {
        if (Statut == StatutAbonnement.Annule)
            throw new InvalidOperationException("Cannot renew a cancelled subscription.");

        DateFin = DateFin.AddMonths(1);
        Statut = StatutAbonnement.Actif;
    }

    public void Annuler()
    {
        Statut = StatutAbonnement.Annule;
    }

    public bool EstExpire() => DateTime.UtcNow > DateFin;

    public void VerifierExpiration()
    {
        if (EstExpire() && Statut == StatutAbonnement.Actif)
        {
            Statut = StatutAbonnement.Expire;
        }
    }
}
