namespace Domain.BillingManagement;

using Domain.BillingManagement.Events;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class Abonnement : AggregateRoot<AbonnementId>
{
    /// <summary>
    /// Grace period in days after subscription expires before downgrade.
    /// </summary>
    public const int GracePeriodJours = 3;

    public string GestionnaireId { get; private set; }
    public PlanAbonnementId PlanId { get; private set; }
    public PlanTarifaire Plan { get; private set; }
    public StatutAbonnement Statut { get; private set; }
    public decimal MontantMensuel { get; private set; }
    public string Currency { get; private set; }
    public DateTime DateDebut { get; private set; }
    public DateTime DateFin { get; private set; }
    public DateTime? DateFinGrace { get; private set; }
    public bool RenouvellementAuto { get; private set; }
    public string? DernierTransactionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Abonnement() : base()
    {
        GestionnaireId = string.Empty;
        PlanId = default!;
        Currency = "XOF";
    }

    private Abonnement(
        AbonnementId id,
        string gestionnaireId,
        PlanAbonnementId planId,
        PlanTarifaire plan,
        decimal montantMensuel,
        string currency,
        DateTime dateDebut,
        DateTime dateFin,
        bool renouvellementAuto) : base(id)
    {
        GestionnaireId = gestionnaireId;
        PlanId = planId;
        Plan = plan;
        MontantMensuel = montantMensuel;
        Currency = currency;
        Statut = StatutAbonnement.Actif;
        DateDebut = dateDebut;
        DateFin = dateFin;
        RenouvellementAuto = renouvellementAuto;
        CreatedAt = DateTime.UtcNow;
    }

    public static Abonnement Create(string gestionnaireId, PlanTarifaire plan)
    {
        if (string.IsNullOrWhiteSpace(gestionnaireId))
            throw new ArgumentException("GestionnaireId cannot be empty.", nameof(gestionnaireId));

        var (montant, planId) = plan switch
        {
            PlanTarifaire.Gratuit => (0m, PlanAbonnement.SeedIds.Gratuit),
            PlanTarifaire.Pro => (2000m, PlanAbonnement.SeedIds.Pro),
            PlanTarifaire.Imf => (0m, PlanAbonnement.SeedIds.Imf),
            _ => throw new ArgumentOutOfRangeException(nameof(plan))
        };

        var now = DateTime.UtcNow;
        // Calendar month billing: end of current calendar month
        var dateFin = plan == PlanTarifaire.Gratuit
            ? DateTime.MaxValue
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        var abonnement = new Abonnement(
            AbonnementId.Create(),
            gestionnaireId,
            PlanAbonnementId.From(planId),
            plan,
            montant,
            "XOF",
            now,
            dateFin,
            renouvellementAuto: plan != PlanTarifaire.Gratuit);

        abonnement.AddDomainEvent(new AbonnementCreatedEvent(abonnement.Id, gestionnaireId, plan));

        return abonnement;
    }

    /// <summary>
    /// Creates a subscription with explicit plan ID reference.
    /// </summary>
    public static Abonnement CreateAvecPlan(
        string gestionnaireId,
        PlanAbonnementId planId,
        PlanTarifaire plan,
        decimal montantMensuel,
        string? transactionId = null)
    {
        if (string.IsNullOrWhiteSpace(gestionnaireId))
            throw new ArgumentException("GestionnaireId cannot be empty.", nameof(gestionnaireId));

        var now = DateTime.UtcNow;
        var dateFin = plan == PlanTarifaire.Gratuit
            ? DateTime.MaxValue
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        var abonnement = new Abonnement(
            AbonnementId.Create(),
            gestionnaireId,
            planId,
            plan,
            montantMensuel,
            "XOF",
            now,
            dateFin,
            renouvellementAuto: plan != PlanTarifaire.Gratuit)
        {
            DernierTransactionId = transactionId
        };

        abonnement.AddDomainEvent(new AbonnementCreatedEvent(abonnement.Id, gestionnaireId, plan));

        return abonnement;
    }

    public void Renouveler(string? transactionId = null)
    {
        if (Statut == StatutAbonnement.Annule)
            throw new InvalidOperationException("Cannot renew a cancelled subscription.");

        // Calendar month billing: move to end of next month from DateFin
        var nextMonth = DateFin == DateTime.MaxValue
            ? DateTime.MaxValue
            : new DateTime(DateFin.Year, DateFin.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        DateFin = nextMonth;
        DateFinGrace = null;
        Statut = StatutAbonnement.Actif;
        DernierTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AbonnementRenouvelleEvent(Id, GestionnaireId, Plan, DateFin));
    }

    public void Annuler()
    {
        Statut = StatutAbonnement.Annule;
        RenouvellementAuto = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool EstExpire() => DateTime.UtcNow > DateFin;

    /// <summary>
    /// Checks if grace period has expired. Returns true if subscription should be downgraded.
    /// </summary>
    public bool EstGraceExpiree() =>
        Statut == StatutAbonnement.EnGrace &&
        DateFinGrace.HasValue &&
        DateTime.UtcNow > DateFinGrace.Value;

    /// <summary>
    /// Moves subscription into grace period (3 days after expiration).
    /// Free plan is never put in grace.
    /// </summary>
    public void PasserEnGrace()
    {
        if (Plan == PlanTarifaire.Gratuit)
            return; // Free plan: never deactivated

        if (Statut != StatutAbonnement.Actif)
            throw new InvalidOperationException("Only active subscriptions can enter grace period.");

        Statut = StatutAbonnement.EnGrace;
        DateFinGrace = DateFin.AddDays(GracePeriodJours);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Expires the subscription after grace period. Downgrades to Gratuit plan.
    /// </summary>
    public void Expirer()
    {
        if (Plan == PlanTarifaire.Gratuit)
            return; // Free plan: never expires

        Statut = StatutAbonnement.Expire;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AbonnementExpireEvent(Id, GestionnaireId, Plan));
    }

    public void VerifierExpiration()
    {
        if (Plan == PlanTarifaire.Gratuit)
            return; // Free plan: never expires

        if (EstExpire() && Statut == StatutAbonnement.Actif)
        {
            PasserEnGrace();
        }
        else if (EstGraceExpiree())
        {
            Expirer();
        }
    }

    /// <summary>
    /// Returns true if the subscription is considered functionally active
    /// (Actif or EnGrace — user can still use the service during grace).
    /// </summary>
    public bool EstFonctionnellementActif() =>
        Statut == StatutAbonnement.Actif || Statut == StatutAbonnement.EnGrace;
}

