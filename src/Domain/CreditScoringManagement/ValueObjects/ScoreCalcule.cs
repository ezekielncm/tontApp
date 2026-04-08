namespace Domain.CreditScoringManagement.ValueObjects;

using Domain.Common;

/// <summary>
/// Value object representing a computed credit score.
/// Score is always clamped between 0 and 100.
/// Formula: (cyclesCompletes × 20) + (tauxPonctualite × 50) + min(ancienneteEnMois, 24)
/// </summary>
public sealed class ScoreCalcule : ValueObject
{
    public int Valeur { get; }
    public int CyclesCompletes { get; }
    public double TauxPonctualite { get; }
    public int AncienneteEnMois { get; }
    public NiveauRisque Niveau { get; }
    public DateTime CalculeLe { get; }

    private ScoreCalcule(
        int valeur,
        int cyclesCompletes,
        double tauxPonctualite,
        int ancienneteEnMois,
        NiveauRisque niveau,
        DateTime calculeLe)
    {
        Valeur = valeur;
        CyclesCompletes = cyclesCompletes;
        TauxPonctualite = tauxPonctualite;
        AncienneteEnMois = ancienneteEnMois;
        Niveau = niveau;
        CalculeLe = calculeLe;
    }

    public static ScoreCalcule Create(
        int cyclesCompletes,
        double tauxPonctualite,
        int ancienneteEnMois)
    {
        if (cyclesCompletes < 0)
            throw new ArgumentException("CyclesCompletes cannot be negative.", nameof(cyclesCompletes));

        if (tauxPonctualite < 0.0 || tauxPonctualite > 1.0)
            throw new ArgumentException("TauxPonctualite must be between 0.0 and 1.0.", nameof(tauxPonctualite));

        if (ancienneteEnMois < 0)
            throw new ArgumentException("AncienneteEnMois cannot be negative.", nameof(ancienneteEnMois));

        var rawScore = (cyclesCompletes * 20)
                       + (int)(tauxPonctualite * 50)
                       + Math.Min(ancienneteEnMois, 24);

        var valeur = Math.Clamp(rawScore, 0, 100);
        var niveau = DeterminerNiveau(valeur);

        return new ScoreCalcule(
            valeur,
            cyclesCompletes,
            tauxPonctualite,
            ancienneteEnMois,
            niveau,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a special "insufficient data" score when less than 1 complete cycle.
    /// </summary>
    public static ScoreCalcule DonneesInsuffisantes()
    {
        return new ScoreCalcule(0, 0, 0.0, 0, NiveauRisque.Faible, DateTime.UtcNow);
    }

    private static NiveauRisque DeterminerNiveau(int score) => score switch
    {
        >= 80 => NiveauRisque.Excellent,
        >= 60 => NiveauRisque.Bon,
        >= 40 => NiveauRisque.Moyen,
        _ => NiveauRisque.Faible
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valeur;
        yield return CyclesCompletes;
        yield return TauxPonctualite;
        yield return AncienneteEnMois;
        yield return Niveau;
        yield return CalculeLe;
    }
}
