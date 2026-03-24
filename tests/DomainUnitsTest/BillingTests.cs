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
