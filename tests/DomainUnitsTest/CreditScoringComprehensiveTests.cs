using Domain.CreditScoringManagement;
using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.Ports;
using Domain.CreditScoringManagement.ValueObjects;
using FluentAssertions;

namespace DomainUnitsTest;

/// <summary>
/// Comprehensive credit scoring tests covering all formula edge cases,
/// boundary values (0 cycles, 100% late, max tenure), and risk levels.
/// Naming convention: MethodName_Scenario_ExpectedResult
/// </summary>
public class CreditScoringComprehensiveTests
{
    private readonly IScoringEngine _scoringEngine = new TestScoringEngine();

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

    // ─── Formula edge cases ────────────────────────────────────────────────

    [Fact]
    public void ScoreCalcule_ZeroCycles_ZeroPonctualite_ZeroAnciennete_ReturnsZero()
    {
        var score = ScoreCalcule.Create(0, 0.0, 0);

        score.Valeur.Should().Be(0);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoreCalcule_OneCycle_ZeroPonctualite_ZeroAnciennete_Returns20()
    {
        // (1×20) + (0.0×50) + min(0,24) = 20
        var score = ScoreCalcule.Create(1, 0.0, 0);

        score.Valeur.Should().Be(20);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoreCalcule_ZeroCycles_FullPonctualite_ZeroAnciennete_Returns50()
    {
        // (0×20) + (1.0×50) + min(0,24) = 50
        var score = ScoreCalcule.Create(0, 1.0, 0);

        score.Valeur.Should().Be(50);
        score.Niveau.Should().Be(NiveauRisque.Moyen);
    }

    [Fact]
    public void ScoreCalcule_ZeroCycles_ZeroPonctualite_MaxAnciennete_Returns24()
    {
        // (0×20) + (0.0×50) + min(24,24) = 24
        var score = ScoreCalcule.Create(0, 0.0, 24);

        score.Valeur.Should().Be(24);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoreCalcule_ZeroCycles_ZeroPonctualite_ExceedMaxAnciennete_CappedAt24()
    {
        // (0×20) + (0.0×50) + min(100,24) = 24
        var score = ScoreCalcule.Create(0, 0.0, 100);

        score.Valeur.Should().Be(24);
    }

    [Fact]
    public void ScoreCalcule_FiveCycles_FullPonctualite_36Months_ClampedAt100()
    {
        // (5×20) + (1.0×50) + min(36,24) = 100 + 50 + 24 = 174 → 100
        var score = ScoreCalcule.Create(5, 1.0, 36);

        score.Valeur.Should().Be(100);
        score.Niveau.Should().Be(NiveauRisque.Excellent);
    }

    [Fact]
    public void ScoreCalcule_100PercentLate_OneCycle_Returns20()
    {
        // 100% late means tauxPonctualite = 0.0
        // (1×20) + (0.0×50) + min(0,24) = 20
        var score = ScoreCalcule.Create(1, 0.0, 0);

        score.Valeur.Should().Be(20);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoreCalcule_50PercentPonctualite_Returns25ForFormula()
    {
        // (0×20) + (0.5×50) + min(0,24) = 0 + 25 + 0 = 25
        var score = ScoreCalcule.Create(0, 0.5, 0);

        score.Valeur.Should().Be(25);
    }

    [Fact]
    public void ScoreCalcule_ExactlyAt80_ReturnsExcellent()
    {
        // (3×20) + (0.4×50) + min(0,24) = 60 + 20 + 0 = 80
        var score = ScoreCalcule.Create(3, 0.4, 0);

        score.Valeur.Should().Be(80);
        score.Niveau.Should().Be(NiveauRisque.Excellent);
    }

    [Fact]
    public void ScoreCalcule_ExactlyAt60_ReturnsBon()
    {
        // (2×20) + (0.4×50) + min(0,24) = 40 + 20 + 0 = 60
        var score = ScoreCalcule.Create(2, 0.4, 0);

        score.Valeur.Should().Be(60);
        score.Niveau.Should().Be(NiveauRisque.Bon);
    }

    [Fact]
    public void ScoreCalcule_ExactlyAt40_ReturnsMoyen()
    {
        // (2×20) + (0.0×50) + min(0,24) = 40
        var score = ScoreCalcule.Create(2, 0.0, 0);

        score.Valeur.Should().Be(40);
        score.Niveau.Should().Be(NiveauRisque.Moyen);
    }

    [Fact]
    public void ScoreCalcule_At79_ReturnsBon()
    {
        // (3×20) + (0.38×50) + min(0,24) = 60 + 19 = 79
        var score = ScoreCalcule.Create(3, 0.38, 0);

        score.Valeur.Should().Be(79);
        score.Niveau.Should().Be(NiveauRisque.Bon);
    }

    [Fact]
    public void ScoreCalcule_At39_ReturnsFaible()
    {
        // (1×20) + (0.38×50) + min(0,24) = 20 + 19 = 39
        var score = ScoreCalcule.Create(1, 0.38, 0);

        score.Valeur.Should().Be(39);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoreCalcule_At59_ReturnsMoyen()
    {
        // (2×20) + (0.38×50) + min(0,24) = 40 + 19 = 59
        var score = ScoreCalcule.Create(2, 0.38, 0);

        score.Valeur.Should().Be(59);
        score.Niveau.Should().Be(NiveauRisque.Moyen);
    }

    // ─── Validation edge cases ─────────────────────────────────────────────

    [Fact]
    public void ScoreCalcule_NegativeCycles_ThrowsArgumentException()
    {
        var act = () => ScoreCalcule.Create(-1, 0.5, 6);

        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("cyclesCompletes");
    }

    [Fact]
    public void ScoreCalcule_PonctualiteBelowZero_ThrowsArgumentException()
    {
        var act = () => ScoreCalcule.Create(1, -0.01, 6);

        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("tauxPonctualite");
    }

    [Fact]
    public void ScoreCalcule_PonctualiteAboveOne_ThrowsArgumentException()
    {
        var act = () => ScoreCalcule.Create(1, 1.01, 6);

        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("tauxPonctualite");
    }

    [Fact]
    public void ScoreCalcule_NegativeAnciennete_ThrowsArgumentException()
    {
        var act = () => ScoreCalcule.Create(1, 0.5, -1);

        act.Should().Throw<ArgumentException>()
           .And.ParamName.Should().Be("ancienneteEnMois");
    }

    [Fact]
    public void ScoreCalcule_ExactlyOnePonctualite_IsValid()
    {
        var score = ScoreCalcule.Create(0, 1.0, 0);

        score.Valeur.Should().Be(50);
    }

    [Fact]
    public void ScoreCalcule_ExactlyZeroPonctualite_IsValid()
    {
        var score = ScoreCalcule.Create(0, 0.0, 0);

        score.Valeur.Should().Be(0);
    }

    // ─── DonneesInsuffisantes ──────────────────────────────────────────────

    [Fact]
    public void DonneesInsuffisantes_ReturnsZeroScore()
    {
        var score = ScoreCalcule.DonneesInsuffisantes();

        score.Valeur.Should().Be(0);
        score.CyclesCompletes.Should().Be(0);
        score.TauxPonctualite.Should().Be(0.0);
        score.AncienneteEnMois.Should().Be(0);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void DonneesInsuffisantes_SetsCalculeLe()
    {
        var before = DateTime.UtcNow;

        var score = ScoreCalcule.DonneesInsuffisantes();

        score.CalculeLe.Should().BeOnOrAfter(before);
        score.CalculeLe.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    // ─── HistoriqueComportement ────────────────────────────────────────────

    [Fact]
    public void HistoriqueComportement_Create_InitializesAllToZero()
    {
        var historique = HistoriqueComportement.Create();

        historique.TotalVersements.Should().Be(0);
        historique.VersementsPonctuels.Should().Be(0);
        historique.CyclesCompletes.Should().Be(0);
    }

    [Fact]
    public void HistoriqueComportement_EnregistrerVersement_Ponctuel_IncrementsBoth()
    {
        var historique = HistoriqueComportement.Create();

        historique.EnregistrerVersement(estPonctuel: true);

        historique.TotalVersements.Should().Be(1);
        historique.VersementsPonctuels.Should().Be(1);
    }

    [Fact]
    public void HistoriqueComportement_EnregistrerVersement_NonPonctuel_IncrementsOnlyTotal()
    {
        var historique = HistoriqueComportement.Create();

        historique.EnregistrerVersement(estPonctuel: false);

        historique.TotalVersements.Should().Be(1);
        historique.VersementsPonctuels.Should().Be(0);
    }

    [Fact]
    public void HistoriqueComportement_IncrementCyclesCompletes_IncrementsByOne()
    {
        var historique = HistoriqueComportement.Create();

        historique.IncrementCyclesCompletes();
        historique.IncrementCyclesCompletes();

        historique.CyclesCompletes.Should().Be(2);
    }

    [Fact]
    public void HistoriqueComportement_CalculerTauxPonctualite_NoVersements_ReturnsZero()
    {
        var historique = HistoriqueComportement.Create();

        historique.CalculerTauxPonctualite().Should().Be(0.0);
    }

    [Fact]
    public void HistoriqueComportement_CalculerTauxPonctualite_AllPonctuel_ReturnsOne()
    {
        var historique = HistoriqueComportement.Create();
        for (int i = 0; i < 10; i++)
            historique.EnregistrerVersement(estPonctuel: true);

        historique.CalculerTauxPonctualite().Should().Be(1.0);
    }

    [Fact]
    public void HistoriqueComportement_CalculerTauxPonctualite_HalfPonctuel_ReturnsFifty()
    {
        var historique = HistoriqueComportement.Create();
        for (int i = 0; i < 5; i++)
            historique.EnregistrerVersement(estPonctuel: true);
        for (int i = 0; i < 5; i++)
            historique.EnregistrerVersement(estPonctuel: false);

        historique.CalculerTauxPonctualite().Should().Be(0.5);
    }

    [Fact]
    public void HistoriqueComportement_CalculerTauxPonctualite_AllLate_ReturnsZero()
    {
        var historique = HistoriqueComportement.Create();
        for (int i = 0; i < 10; i++)
            historique.EnregistrerVersement(estPonctuel: false);

        historique.CalculerTauxPonctualite().Should().Be(0.0);
    }

    [Fact]
    public void HistoriqueComportement_CalculerAncienneteEnMois_NewMember_ReturnsZero()
    {
        var historique = CreateHistorique(0, 0, 0, 0);

        historique.CalculerAncienneteEnMois().Should().Be(0);
    }

    [Fact]
    public void HistoriqueComportement_CalculerAncienneteEnMois_12MonthsAgo_Returns12()
    {
        var historique = CreateHistorique(0, 0, 0, 12);

        historique.CalculerAncienneteEnMois().Should().Be(12);
    }

    // ─── ScoringEngine via HistoriqueComportement ──────────────────────────

    [Fact]
    public void ScoringEngine_ZeroCyclesHistory_ReturnsZero()
    {
        var historique = CreateHistorique(10, 10, 0, 12);

        var score = _scoringEngine.Calculer(historique);

        // (0×20) + (1.0×50) + min(12,24) = 0 + 50 + 12 = 62
        score.Valeur.Should().Be(62);
    }

    [Fact]
    public void ScoringEngine_AllLatePayments_OneCycle_ReturnsLow()
    {
        var historique = CreateHistorique(10, 0, 1, 6);

        var score = _scoringEngine.Calculer(historique);

        // (1×20) + (0.0×50) + min(6,24) = 20 + 0 + 6 = 26
        score.Valeur.Should().Be(26);
        score.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ScoringEngine_PerfectScore_HighValues_ClampedAt100()
    {
        var historique = CreateHistorique(50, 50, 10, 36);

        var score = _scoringEngine.Calculer(historique);

        // (10×20) + (1.0×50) + min(36,24) = 200 + 50 + 24 = 274 → 100
        score.Valeur.Should().Be(100);
        score.Niveau.Should().Be(NiveauRisque.Excellent);
    }

    [Fact]
    public void ScoringEngine_MixedPayments_TwoCycles_ReturnsCorrectScore()
    {
        var historique = CreateHistorique(10, 8, 2, 6);

        var score = _scoringEngine.Calculer(historique);

        // (2×20) + (0.8×50) + min(6,24) = 40 + 40 + 6 = 86
        score.Valeur.Should().Be(86);
        score.Niveau.Should().Be(NiveauRisque.Excellent);
    }

    // ─── ProfilCredit aggregate ────────────────────────────────────────────

    [Fact]
    public void ProfilCredit_Create_WithEmptyGuid_ThrowsArgumentException()
    {
        var act = () => ProfilCredit.Create(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProfilCredit_Create_WithValidGuid_InitializesWithInsufficientData()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());

        profil.DonneesInsuffisantes.Should().BeTrue();
        profil.ScoreActuel.Valeur.Should().Be(0);
    }

    [Fact]
    public void ProfilCredit_EnregistrerVersement_WithoutCycles_StaysInsufficient()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());

        profil.EnregistrerVersementConfirme(true, _scoringEngine);

        profil.DonneesInsuffisantes.Should().BeTrue();
        profil.ScoreActuel.Valeur.Should().Be(0);
    }

    [Fact]
    public void ProfilCredit_IncrementCycle_ScoreBecomesAvailable()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());
        for (int i = 0; i < 5; i++)
            profil.EnregistrerVersementConfirme(true, _scoringEngine);

        profil.IncrementCycleComplete(_scoringEngine);

        profil.DonneesInsuffisantes.Should().BeFalse();
        profil.ScoreActuel.Valeur.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProfilCredit_RecalculerScore_WithZeroCycles_ReturnsInsufficient()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());

        profil.RecalculerScore(_scoringEngine);

        profil.DonneesInsuffisantes.Should().BeTrue();
    }

    [Fact]
    public void ProfilCredit_MultipleCycles_ScoreIncreases()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());
        for (int i = 0; i < 5; i++)
            profil.EnregistrerVersementConfirme(true, _scoringEngine);
        profil.IncrementCycleComplete(_scoringEngine);

        var scoreAfterOneCycle = profil.ScoreActuel.Valeur;

        for (int i = 0; i < 5; i++)
            profil.EnregistrerVersementConfirme(true, _scoringEngine);
        profil.IncrementCycleComplete(_scoringEngine);

        profil.ScoreActuel.Valeur.Should().BeGreaterThan(scoreAfterOneCycle);
    }

    [Fact]
    public void ProfilCredit_AllLatePayments_OneCycle_HasLowScore()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());
        for (int i = 0; i < 5; i++)
            profil.EnregistrerVersementConfirme(false, _scoringEngine);
        profil.IncrementCycleComplete(_scoringEngine);

        // (1×20) + (0.0×50) + min(0,24) = 20
        profil.ScoreActuel.Valeur.Should().Be(20);
        profil.ScoreActuel.Niveau.Should().Be(NiveauRisque.Faible);
    }

    [Fact]
    public void ProfilCredit_UpdatedAt_ChangesAfterRecalculation()
    {
        var profil = ProfilCredit.Create(Guid.NewGuid());
        var initialUpdatedAt = profil.UpdatedAt;

        profil.RecalculerScore(_scoringEngine);

        profil.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void ProfilCredit_MembreId_IsSetCorrectly()
    {
        var membreId = Guid.NewGuid();

        var profil = ProfilCredit.Create(membreId);

        profil.MembreId.Should().Be(membreId);
    }

    // ─── ScoreCalcule metadata ─────────────────────────────────────────────

    [Fact]
    public void ScoreCalcule_Create_StoresInputComponents()
    {
        var score = ScoreCalcule.Create(2, 0.8, 12);

        score.CyclesCompletes.Should().Be(2);
        score.TauxPonctualite.Should().Be(0.8);
        score.AncienneteEnMois.Should().Be(12);
    }

    [Fact]
    public void ScoreCalcule_Create_SetsCalculeLe()
    {
        var before = DateTime.UtcNow;

        var score = ScoreCalcule.Create(1, 0.5, 6);

        score.CalculeLe.Should().BeOnOrAfter(before);
        score.CalculeLe.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    // ─── ProfilCreditId ────────────────────────────────────────────────────

    [Fact]
    public void ProfilCreditId_Create_GeneratesUniqueIds()
    {
        var id1 = ProfilCreditId.Create();
        var id2 = ProfilCreditId.Create();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ProfilCreditId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => ProfilCreditId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HistoriqueComportementId_Create_GeneratesUniqueIds()
    {
        var id1 = HistoriqueComportementId.Create();
        var id2 = HistoriqueComportementId.Create();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void HistoriqueComportementId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => HistoriqueComportementId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
