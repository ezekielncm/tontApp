namespace Domain.BillingManagement.ValueObjects;

using Domain.Common;

public sealed class ScoreCredit : ValueObject
{
    public int CyclesCompletes { get; }
    public decimal TauxPonctualite { get; }
    public int AncienneteMois { get; }
    public int Score { get; }
    public string Niveau { get; }

    private ScoreCredit(int cyclesCompletes, decimal tauxPonctualite, int ancienneteMois)
    {
        CyclesCompletes = cyclesCompletes;
        TauxPonctualite = tauxPonctualite;
        AncienneteMois = ancienneteMois;

        Score = (cyclesCompletes * 20) + (int)(tauxPonctualite * 50) + Math.Min(ancienneteMois, 24);

        Niveau = Score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Bon",
            >= 40 => "Moyen",
            _ => "Faible"
        };
    }

    public static ScoreCredit Create(int cyclesCompletes, decimal tauxPonctualite, int ancienneteMois)
    {
        if (cyclesCompletes < 0)
            throw new ArgumentException("CyclesCompletes must be >= 0.", nameof(cyclesCompletes));

        if (tauxPonctualite < 0 || tauxPonctualite > 1)
            throw new ArgumentException("TauxPonctualite must be between 0 and 1.", nameof(tauxPonctualite));

        if (ancienneteMois < 0)
            throw new ArgumentException("AncienneteMois must be >= 0.", nameof(ancienneteMois));

        return new ScoreCredit(cyclesCompletes, tauxPonctualite, ancienneteMois);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CyclesCompletes;
        yield return TauxPonctualite;
        yield return AncienneteMois;
    }
}
