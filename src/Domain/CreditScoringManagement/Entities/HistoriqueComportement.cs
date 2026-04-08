namespace Domain.CreditScoringManagement.Entities;

using Domain.Common;
using Domain.CreditScoringManagement.ValueObjects;

/// <summary>
/// Entity tracking a member's behavioral history for credit scoring.
/// Updated on each VersementConfirmeEvent.
/// </summary>
public class HistoriqueComportement : Entity<HistoriqueComportementId>
{
    public int TotalVersements { get; private set; }
    public int VersementsPonctuels { get; private set; }
    public int CyclesCompletes { get; private set; }
    public DateTime DatePremierVersement { get; private set; }
    public DateTime DernierVersement { get; private set; }

    private HistoriqueComportement() : base()
    {
    }

    public HistoriqueComportement(
        HistoriqueComportementId id,
        int totalVersements,
        int versementsPonctuels,
        int cyclesCompletes,
        DateTime datePremierVersement,
        DateTime dernierVersement) : base(id)
    {
        TotalVersements = totalVersements;
        VersementsPonctuels = versementsPonctuels;
        CyclesCompletes = cyclesCompletes;
        DatePremierVersement = datePremierVersement;
        DernierVersement = dernierVersement;
    }

    public static HistoriqueComportement Create()
    {
        return new HistoriqueComportement(
            HistoriqueComportementId.Create(),
            totalVersements: 0,
            versementsPonctuels: 0,
            cyclesCompletes: 0,
            datePremierVersement: DateTime.UtcNow,
            dernierVersement: DateTime.UtcNow);
    }

    /// <summary>
    /// Records a confirmed payment. estPonctuel indicates whether the payment was on time.
    /// </summary>
    public void EnregistrerVersement(bool estPonctuel)
    {
        TotalVersements++;
        if (estPonctuel)
            VersementsPonctuels++;
        DernierVersement = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the number of completed cycles (tontine rounds).
    /// </summary>
    public void IncrementCyclesCompletes()
    {
        CyclesCompletes++;
    }

    /// <summary>
    /// Computes the on-time rate (0.0 – 1.0). Returns 0 if no payments yet.
    /// </summary>
    public double CalculerTauxPonctualite()
    {
        return TotalVersements == 0
            ? 0.0
            : (double)VersementsPonctuels / TotalVersements;
    }

    /// <summary>
    /// Computes membership age in months from the first payment.
    /// </summary>
    public int CalculerAncienneteEnMois()
    {
        var now = DateTime.UtcNow;
        return ((now.Year - DatePremierVersement.Year) * 12) + now.Month - DatePremierVersement.Month;
    }
}
