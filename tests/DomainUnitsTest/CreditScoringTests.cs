using Domain.CreditScoringManagement;
using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.Ports;
using Domain.CreditScoringManagement.ValueObjects;
using Infrastructure.CreditScoring;

namespace DomainUnitsTest;

public class CreditScoringTests
{
    private readonly IScoringEngine _scoringEngine = new RegleMetierScoringEngine();

    /// <summary>
    /// Helper to create HistoriqueComportement with specific values for testing.
    /// Uses reflection-free approach via the public API.
    /// </summary>
    private static HistoriqueComportement CreateHistorique(
        int totalVersements,
        int versementsPonctuels,
        int cyclesCompletes,
        int ancienneteEnMois)
    {
        var datePremier = DateTime.UtcNow.AddMonths(-ancienneteEnMois);
        return new HistoriqueComportement(
            HistoriqueComportementId.Create(),
            totalVersements,
            versementsPonctuels,
            cyclesCompletes,
            datePremier,
            DateTime.UtcNow);
    }

    // ─── Test 1: Zero everything → score = 0 ─────────────────────────────────
    [Fact]
    public void Score_ZeroEverything_ReturnsZero()
    {
        var score = ScoreCalcule.Create(cyclesCompletes: 0, tauxPonctualite: 0.0, ancienneteEnMois: 0);
        Assert.Equal(0, score.Valeur);
    }

    // ─── Test 2: 1 cycle, 100% punctuality, 0 months → score = 70 ─────────────
    [Fact]
    public void Score_OneCycle_FullPunctuality_ZeroMonths_Returns70()
    {
        // (1×20) + (1.0×50) + min(0,24) = 20 + 50 + 0 = 70
        var score = ScoreCalcule.Create(cyclesCompletes: 1, tauxPonctualite: 1.0, ancienneteEnMois: 0);
        Assert.Equal(70, score.Valeur);
    }

    // ─── Test 3: Maximum score clamp at 100 ───────────────────────────────────
    [Fact]
    public void Score_HighValues_ClampedAt100()
    {
        // (5×20) + (1.0×50) + min(36,24) = 100 + 50 + 24 = 174 → clamped to 100
        var score = ScoreCalcule.Create(cyclesCompletes: 5, tauxPonctualite: 1.0, ancienneteEnMois: 36);
        Assert.Equal(100, score.Valeur);
    }

    // ─── Test 4: Score never negative ─────────────────────────────────────────
    [Fact]
    public void Score_AllZero_NeverNegative()
    {
        var score = ScoreCalcule.Create(cyclesCompletes: 0, tauxPonctualite: 0.0, ancienneteEnMois: 0);
        Assert.True(score.Valeur >= 0);
    }

    // ─── Test 5: Anciennete capped at 24 ──────────────────────────────────────
    [Fact]
    public void Score_AncienneteCappedAt24()
    {
        // (0×20) + (0.0×50) + min(48,24) = 0 + 0 + 24 = 24
        var score = ScoreCalcule.Create(cyclesCompletes: 0, tauxPonctualite: 0.0, ancienneteEnMois: 48);
        Assert.Equal(24, score.Valeur);
    }

    // ─── Test 6: NiveauRisque Excellent (≥80) ─────────────────────────────────
    [Fact]
    public void Score_80_ReturnsExcellent()
    {
        // (3×20) + (0.4×50) + min(0,24) = 60 + 20 + 0 = 80
        var score = ScoreCalcule.Create(cyclesCompletes: 3, tauxPonctualite: 0.4, ancienneteEnMois: 0);
        Assert.Equal(80, score.Valeur);
        Assert.Equal(NiveauRisque.Excellent, score.Niveau);
    }

    // ─── Test 7: NiveauRisque Bon (60–79) ─────────────────────────────────────
    [Fact]
    public void Score_60to79_ReturnsBon()
    {
        // (2×20) + (0.4×50) + min(0,24) = 40 + 20 + 0 = 60
        var score = ScoreCalcule.Create(cyclesCompletes: 2, tauxPonctualite: 0.4, ancienneteEnMois: 0);
        Assert.Equal(60, score.Valeur);
        Assert.Equal(NiveauRisque.Bon, score.Niveau);
    }

    // ─── Test 8: NiveauRisque Moyen (40–59) ───────────────────────────────────
    [Fact]
    public void Score_40to59_ReturnsMoyen()
    {
        // (2×20) + (0.0×50) + min(0,24) = 40 + 0 + 0 = 40
        var score = ScoreCalcule.Create(cyclesCompletes: 2, tauxPonctualite: 0.0, ancienneteEnMois: 0);
        Assert.Equal(40, score.Valeur);
        Assert.Equal(NiveauRisque.Moyen, score.Niveau);
    }

    // ─── Test 9: NiveauRisque Faible (<40) ────────────────────────────────────
    [Fact]
    public void Score_Below40_ReturnsFaible()
    {
        // (1×20) + (0.0×50) + min(0,24) = 20 + 0 + 0 = 20
        var score = ScoreCalcule.Create(cyclesCompletes: 1, tauxPonctualite: 0.0, ancienneteEnMois: 0);
        Assert.Equal(20, score.Valeur);
        Assert.Equal(NiveauRisque.Faible, score.Niveau);
    }

    // ─── Test 10: DonneesInsuffisantes returns zero ───────────────────────────
    [Fact]
    public void DonneesInsuffisantes_ReturnsZeroScore()
    {
        var score = ScoreCalcule.DonneesInsuffisantes();
        Assert.Equal(0, score.Valeur);
        Assert.Equal(NiveauRisque.Faible, score.Niveau);
    }

    // ─── Test 11: Full formula edge case (2 cycles, 80% punctual, 12 months) ─
    [Fact]
    public void Score_TwoCycles_80Percent_12Months_Returns92()
    {
        // (2×20) + (0.8×50) + min(12,24) = 40 + 40 + 12 = 92
        var score = ScoreCalcule.Create(cyclesCompletes: 2, tauxPonctualite: 0.8, ancienneteEnMois: 12);
        Assert.Equal(92, score.Valeur);
        Assert.Equal(NiveauRisque.Excellent, score.Niveau);
    }

    // ─── Test 12: Only anciennete contributes ─────────────────────────────────
    [Fact]
    public void Score_OnlyAnciennete_Returns24()
    {
        // (0×20) + (0.0×50) + min(24,24) = 0 + 0 + 24 = 24
        var score = ScoreCalcule.Create(cyclesCompletes: 0, tauxPonctualite: 0.0, ancienneteEnMois: 24);
        Assert.Equal(24, score.Valeur);
    }

    // ─── Test 13: Only punctuality contributes ────────────────────────────────
    [Fact]
    public void Score_OnlyPunctuality_Returns50()
    {
        // (0×20) + (1.0×50) + min(0,24) = 0 + 50 + 0 = 50
        var score = ScoreCalcule.Create(cyclesCompletes: 0, tauxPonctualite: 1.0, ancienneteEnMois: 0);
        Assert.Equal(50, score.Valeur);
    }

    // ─── Test 14: Only cycles contribute ──────────────────────────────────────
    [Fact]
    public void Score_OnlyCycles_Returns40()
    {
        // (2×20) + (0.0×50) + min(0,24) = 40 + 0 + 0 = 40
        var score = ScoreCalcule.Create(cyclesCompletes: 2, tauxPonctualite: 0.0, ancienneteEnMois: 0);
        Assert.Equal(40, score.Valeur);
    }

    // ─── Test 15: Negative cyclesCompletes throws ─────────────────────────────
    [Fact]
    public void Score_NegativeCycles_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScoreCalcule.Create(cyclesCompletes: -1, tauxPonctualite: 0.5, ancienneteEnMois: 6));
    }

    // ─── Test 16: TauxPonctualite out of range throws ─────────────────────────
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Score_InvalidPunctuality_Throws(double tauxPonctualite)
    {
        Assert.Throws<ArgumentException>(() =>
            ScoreCalcule.Create(cyclesCompletes: 1, tauxPonctualite: tauxPonctualite, ancienneteEnMois: 6));
    }

    // ─── Test 17: Negative anciennete throws ──────────────────────────────────
    [Fact]
    public void Score_NegativeAnciennete_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScoreCalcule.Create(cyclesCompletes: 1, tauxPonctualite: 0.5, ancienneteEnMois: -1));
    }

    // ─── Test 18: ScoringEngine via HistoriqueComportement ────────────────────
    [Fact]
    public void ScoringEngine_CalculesFromHistorique()
    {
        var historique = CreateHistorique(
            totalVersements: 10,
            versementsPonctuels: 8,
            cyclesCompletes: 2,
            ancienneteEnMois: 6);

        var score = _scoringEngine.Calculer(historique);

        // (2×20) + (0.8×50) + min(6,24) = 40 + 40 + 6 = 86
        Assert.Equal(86, score.Valeur);
        Assert.Equal(NiveauRisque.Excellent, score.Niveau);
    }

    // ─── Test 19: ProfilCredit with insufficient data (<1 cycle) ──────────────
    [Fact]
    public void ProfilCredit_LessThanOneCycle_ShowsInsufficientData()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());
        profil.EnregistrerVersementConfirme(estPonctuel: true, _scoringEngine);

        Assert.True(profil.DonneesInsuffisantes);
        Assert.Equal(0, profil.ScoreActuel.Valeur);
    }

    // ─── Test 20: ProfilCredit score recalculated after cycle complete ────────
    [Fact]
    public void ProfilCredit_AfterCycleComplete_ScoreRecalculated()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());

        // Record 5 punctual payments
        for (int i = 0; i < 5; i++)
            profil.EnregistrerVersementConfirme(estPonctuel: true, _scoringEngine);

        // Still insufficient data (0 complete cycles)
        Assert.True(profil.DonneesInsuffisantes);

        // Complete a cycle
        profil.IncrementCycleComplete(_scoringEngine);

        Assert.False(profil.DonneesInsuffisantes);
        Assert.True(profil.ScoreActuel.Valeur > 0);
        // (1×20) + (1.0×50) + min(0,24) = 20 + 50 + 0 = 70
        Assert.Equal(70, profil.ScoreActuel.Valeur);
    }
}
