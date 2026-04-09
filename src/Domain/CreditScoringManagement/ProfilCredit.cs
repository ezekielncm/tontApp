namespace Domain.CreditScoringManagement;

using Domain.Common;
using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.Ports;
using Domain.CreditScoringManagement.ValueObjects;

/// <summary>
/// Aggregate root for a member's credit profile.
/// Tracks behavioral history and computes credit score.
/// Recalculated asynchronously on each VersementConfirmeEvent.
/// </summary>
public class ProfilCredit : AggregateRoot<ProfilCreditId>
{
    public Guid MembreId { get; private set; }
    public ScoreCalcule ScoreActuel { get; private set; }
    public HistoriqueComportement Historique { get; private set; }
    public bool DonneesInsuffisantes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProfilCredit() : base()
    {
        ScoreActuel = default!;
        Historique = default!;
    }

    private ProfilCredit(
        ProfilCreditId id,
        Guid membreId,
        HistoriqueComportement historique) : base(id)
    {
        MembreId = membreId;
        Historique = historique;
        ScoreActuel = ScoreCalcule.DonneesInsuffisantes();
        DonneesInsuffisantes = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new credit profile for a member.
    /// </summary>
    public static ProfilCredit Create(Guid membreId)
    {
        if (membreId == Guid.Empty)
            throw new ArgumentException("MembreId cannot be empty.", nameof(membreId));

        return new ProfilCredit(
            ProfilCreditId.Create(),
            membreId,
            HistoriqueComportement.Create());
    }

    /// <summary>
    /// Records a confirmed payment and recalculates the score.
    /// </summary>
    public void EnregistrerVersementConfirme(bool estPonctuel, IScoringEngine scoringEngine)
    {
        Historique.EnregistrerVersement(estPonctuel);
        RecalculerScore(scoringEngine);
    }

    /// <summary>
    /// Increments completed cycles and recalculates the score.
    /// </summary>
    public void IncrementCycleComplete(IScoringEngine scoringEngine)
    {
        Historique.IncrementCyclesCompletes();
        RecalculerScore(scoringEngine);
    }

    /// <summary>
    /// Recalculates the score using the provided scoring engine.
    /// </summary>
    public void RecalculerScore(IScoringEngine scoringEngine)
    {
        if (Historique.CyclesCompletes < 1)
        {
            ScoreActuel = ScoreCalcule.DonneesInsuffisantes();
            DonneesInsuffisantes = true;
        }
        else
        {
            ScoreActuel = scoringEngine.Calculer(Historique);
            DonneesInsuffisantes = false;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
