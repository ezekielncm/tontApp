using Domain.BillingManagement;
using Domain.BillingManagement.Events;
using Domain.BillingManagement.ValueObjects;

namespace DomainUnitsTest;

public class AbonnementTests
{
    [Fact]
    public void Create_Pro_SetsCorrectMontant()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        Assert.Equal(2000m, abonnement.MontantMensuel);
        Assert.Equal("XOF", abonnement.Currency);
        Assert.Equal(PlanTarifaire.Pro, abonnement.Plan);
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
    }

    [Fact]
    public void Create_Gratuit_SetsMontantZero()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Gratuit);

        Assert.Equal(0m, abonnement.MontantMensuel);
        Assert.Equal(DateTime.MaxValue, abonnement.DateFin);
    }

    [Fact]
    public void Create_RaisesAbonnementCreatedEvent()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        var domainEvent = Assert.Single(abonnement.DomainEvents);
        var createdEvent = Assert.IsType<AbonnementCreatedEvent>(domainEvent);
        Assert.Equal("gestionnaire-1", createdEvent.GestionnaireId);
        Assert.Equal(PlanTarifaire.Pro, createdEvent.Plan);
    }

    [Fact]
    public void Renouveler_ExtendsDateFin()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        var originalDateFin = abonnement.DateFin;

        abonnement.Renouveler();

        Assert.True(abonnement.DateFin > originalDateFin);
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
    }

    [Fact]
    public void Annuler_SetsStatutAnnule()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.Annuler();

        Assert.Equal(StatutAbonnement.Annule, abonnement.Statut);
    }

    [Fact]
    public void Renouveler_WhenAnnule_ThrowsInvalidOperationException()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.Annuler();

        Assert.Throws<InvalidOperationException>(() => abonnement.Renouveler());
    }

    [Fact]
    public void Create_Pro_SetsCalendarMonthDateFin()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        // DateFin should be the first day of next month (calendar month billing)
        var now = DateTime.UtcNow;
        var expectedDateFin = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        Assert.Equal(expectedDateFin, abonnement.DateFin);
    }

    [Fact]
    public void Create_Pro_SetsRenouvellementAutoTrue()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        Assert.True(abonnement.RenouvellementAuto);
    }

    [Fact]
    public void Create_Gratuit_SetsRenouvellementAutoFalse()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Gratuit);

        Assert.False(abonnement.RenouvellementAuto);
    }

    [Fact]
    public void Create_SetsPlanId()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        Assert.Equal(PlanAbonnement.SeedIds.Pro, abonnement.PlanId.Value);
    }

    [Fact]
    public void Renouveler_SetsTransactionId()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.Renouveler("tx-123");

        Assert.Equal("tx-123", abonnement.DernierTransactionId);
    }

    [Fact]
    public void Renouveler_RaisesAbonnementRenouvelleEvent()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.ClearDomainEvents(); // clear create event

        abonnement.Renouveler();

        var domainEvent = Assert.Single(abonnement.DomainEvents);
        var renewEvent = Assert.IsType<AbonnementRenouvelleEvent>(domainEvent);
        Assert.Equal("gestionnaire-1", renewEvent.GestionnaireId);
        Assert.Equal(PlanTarifaire.Pro, renewEvent.Plan);
    }

    [Fact]
    public void Renouveler_ClearsGracePeriod()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.Renouveler();

        Assert.Null(abonnement.DateFinGrace);
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
    }

    [Fact]
    public void PasserEnGrace_SetsEnGraceStatut()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.PasserEnGrace();

        Assert.Equal(StatutAbonnement.EnGrace, abonnement.Statut);
        Assert.NotNull(abonnement.DateFinGrace);
    }

    [Fact]
    public void PasserEnGrace_Gratuit_DoesNothing()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Gratuit);

        abonnement.PasserEnGrace();

        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
        Assert.Null(abonnement.DateFinGrace);
    }

    [Fact]
    public void PasserEnGrace_WhenNotActif_ThrowsInvalidOperationException()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.Annuler();

        Assert.Throws<InvalidOperationException>(() => abonnement.PasserEnGrace());
    }

    [Fact]
    public void PasserEnGrace_SetsGracePeriod3Days()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.PasserEnGrace();

        Assert.NotNull(abonnement.DateFinGrace);
        var expectedGraceEnd = abonnement.DateFin.AddDays(Abonnement.GracePeriodJours);
        Assert.Equal(expectedGraceEnd, abonnement.DateFinGrace);
    }

    [Fact]
    public void Expirer_SetsStatutExpire()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        abonnement.Expirer();

        Assert.Equal(StatutAbonnement.Expire, abonnement.Statut);
    }

    [Fact]
    public void Expirer_Gratuit_DoesNothing()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Gratuit);

        abonnement.Expirer();

        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
    }

    [Fact]
    public void Expirer_RaisesAbonnementExpireEvent()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.ClearDomainEvents();

        abonnement.Expirer();

        var domainEvent = Assert.Single(abonnement.DomainEvents);
        Assert.IsType<AbonnementExpireEvent>(domainEvent);
    }

    [Fact]
    public void EstFonctionnellementActif_Actif_ReturnsTrue()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);

        Assert.True(abonnement.EstFonctionnellementActif());
    }

    [Fact]
    public void EstFonctionnellementActif_EnGrace_ReturnsTrue()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.PasserEnGrace();

        Assert.True(abonnement.EstFonctionnellementActif());
    }

    [Fact]
    public void EstFonctionnellementActif_Expire_ReturnsFalse()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.Expirer();

        Assert.False(abonnement.EstFonctionnellementActif());
    }

    [Fact]
    public void EstFonctionnellementActif_Annule_ReturnsFalse()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        abonnement.Annuler();

        Assert.False(abonnement.EstFonctionnellementActif());
    }

    [Fact]
    public void Annuler_DisablesAutoRenewal()
    {
        var abonnement = Abonnement.Create("gestionnaire-1", PlanTarifaire.Pro);
        Assert.True(abonnement.RenouvellementAuto);

        abonnement.Annuler();

        Assert.False(abonnement.RenouvellementAuto);
    }

    [Fact]
    public void CreateAvecPlan_SetsAllFields()
    {
        var planId = PlanAbonnementId.From(PlanAbonnement.SeedIds.Pro);
        var abonnement = Abonnement.CreateAvecPlan(
            "gestionnaire-1",
            planId,
            PlanTarifaire.Pro,
            2000m,
            "tx-456");

        Assert.Equal("gestionnaire-1", abonnement.GestionnaireId);
        Assert.Equal(planId, abonnement.PlanId);
        Assert.Equal(PlanTarifaire.Pro, abonnement.Plan);
        Assert.Equal(2000m, abonnement.MontantMensuel);
        Assert.Equal("tx-456", abonnement.DernierTransactionId);
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
    }

    [Fact]
    public void Create_WithEmptyGestionnaireId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Abonnement.Create("", PlanTarifaire.Pro));
    }
}

public class PlanAbonnementTests
{
    [Fact]
    public void Create_WithValidParams_SetsAllProperties()
    {
        var plan = PlanAbonnement.Create(
            "Pro", "PRO", 2000m, "XOF", 10, int.MaxValue, "Plan Pro");

        Assert.Equal("Pro", plan.Nom);
        Assert.Equal("PRO", plan.Code);
        Assert.Equal(2000m, plan.PrixMensuel);
        Assert.Equal("XOF", plan.Devise);
        Assert.Equal(10, plan.MaxTontines);
        Assert.Equal(int.MaxValue, plan.MaxMembresParTontine);
        Assert.Equal("Plan Pro", plan.Description);
        Assert.True(plan.EstActif);
    }

    [Fact]
    public void Create_WithEmptyNom_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PlanAbonnement.Create("", "PRO", 2000m, "XOF", 10, 100));
    }

    [Fact]
    public void Create_WithEmptyCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PlanAbonnement.Create("Pro", "", 2000m, "XOF", 10, 100));
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PlanAbonnement.Create("Pro", "PRO", -1m, "XOF", 10, 100));
    }

    [Fact]
    public void Create_WithNegativeMaxTontines_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PlanAbonnement.Create("Pro", "PRO", 2000m, "XOF", -1, 100));
    }

    [Fact]
    public void Create_WithNegativeMaxMembres_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PlanAbonnement.Create("Pro", "PRO", 2000m, "XOF", 10, -1));
    }

    [Fact]
    public void CreateWithId_SetsSpecificId()
    {
        var id = Guid.NewGuid();
        var plan = PlanAbonnement.CreateWithId(id, "Test", "TEST", 100m, "XOF", 5, 50);

        Assert.Equal(id, plan.Id.Value);
    }

    [Fact]
    public void Desactiver_SetsEstActifFalse()
    {
        var plan = PlanAbonnement.Create("Pro", "PRO", 2000m, "XOF", 10, 100);

        plan.Desactiver();

        Assert.False(plan.EstActif);
    }

    [Fact]
    public void Activer_SetsEstActifTrue()
    {
        var plan = PlanAbonnement.Create("Pro", "PRO", 2000m, "XOF", 10, 100);
        plan.Desactiver();

        plan.Activer();

        Assert.True(plan.EstActif);
    }

    [Fact]
    public void SeedIds_AreNotEmpty()
    {
        Assert.NotEqual(Guid.Empty, PlanAbonnement.SeedIds.Gratuit);
        Assert.NotEqual(Guid.Empty, PlanAbonnement.SeedIds.Pro);
        Assert.NotEqual(Guid.Empty, PlanAbonnement.SeedIds.Imf);
    }

    [Fact]
    public void SeedIds_AreDistinct()
    {
        Assert.NotEqual(PlanAbonnement.SeedIds.Gratuit, PlanAbonnement.SeedIds.Pro);
        Assert.NotEqual(PlanAbonnement.SeedIds.Pro, PlanAbonnement.SeedIds.Imf);
        Assert.NotEqual(PlanAbonnement.SeedIds.Gratuit, PlanAbonnement.SeedIds.Imf);
    }

    [Fact]
    public void Codes_AreCorrectValues()
    {
        Assert.Equal("GRATUIT", PlanAbonnement.Codes.Gratuit);
        Assert.Equal("PRO", PlanAbonnement.Codes.Pro);
        Assert.Equal("IMF", PlanAbonnement.Codes.Imf);
    }
}

public class ScoreCreditTests
{
    [Fact]
    public void Create_WithValidParams_ComputesCorrectScore()
    {
        // Score = (3 * 20) + (int)(0.8 * 50) + Min(12, 24) = 60 + 40 + 12 = 112
        var score = ScoreCredit.Create(3, 0.8m, 12);

        Assert.Equal(112, score.Score);
        Assert.Equal("Excellent", score.Niveau);
    }

    [Fact]
    public void Niveau_Excellent_WhenScoreGreaterOrEqual80()
    {
        // Score = (4 * 20) + (int)(1.0 * 50) + Min(24, 24) = 80 + 50 + 24 = 154
        var score = ScoreCredit.Create(4, 1.0m, 24);

        Assert.Equal("Excellent", score.Niveau);
        Assert.True(score.Score >= 80);
    }

    [Fact]
    public void Niveau_Bon_WhenScore60To79()
    {
        // Score = (2 * 20) + (int)(0.5 * 50) + Min(5, 24) = 40 + 25 + 5 = 70
        var score = ScoreCredit.Create(2, 0.5m, 5);

        Assert.Equal("Bon", score.Niveau);
        Assert.True(score.Score >= 60 && score.Score < 80);
    }

    [Fact]
    public void Niveau_Moyen_WhenScore40To59()
    {
        // Score = (1 * 20) + (int)(0.5 * 50) + Min(5, 24) = 20 + 25 + 5 = 50
        var score = ScoreCredit.Create(1, 0.5m, 5);

        Assert.Equal("Moyen", score.Niveau);
        Assert.True(score.Score >= 40 && score.Score < 60);
    }

    [Fact]
    public void Niveau_Faible_WhenScoreLessThan40()
    {
        // Score = (0 * 20) + (int)(0.5 * 50) + Min(5, 24) = 0 + 25 + 5 = 30
        var score = ScoreCredit.Create(0, 0.5m, 5);

        Assert.Equal("Faible", score.Niveau);
        Assert.True(score.Score < 40);
    }

    [Fact]
    public void Create_WithNegativeCycles_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ScoreCredit.Create(-1, 0.5m, 5));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_WithInvalidTauxPonctualite_ThrowsArgumentException(decimal taux)
    {
        Assert.Throws<ArgumentException>(() => ScoreCredit.Create(1, taux, 5));
    }

    [Fact]
    public void Create_WithNegativeAnciennete_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ScoreCredit.Create(1, 0.5m, -1));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = ScoreCredit.Create(3, 0.8m, 12);
        var b = ScoreCredit.Create(3, 0.8m, 12);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = ScoreCredit.Create(3, 0.8m, 12);
        var b = ScoreCredit.Create(2, 0.8m, 12);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AncienneteMois_CappedAt24()
    {
        // Score = (0 * 20) + (int)(0 * 50) + Min(100, 24) = 0 + 0 + 24 = 24
        var score = ScoreCredit.Create(0, 0m, 100);

        Assert.Equal(24, score.Score);
    }
}
