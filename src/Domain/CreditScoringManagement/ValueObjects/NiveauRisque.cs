namespace Domain.CreditScoringManagement.ValueObjects;

/// <summary>
/// Risk level based on credit score.
/// Excellent: 80-100, Bon: 60-79, Moyen: 40-59, Faible: 0-39
/// </summary>
public enum NiveauRisque
{
    Excellent,
    Bon,
    Moyen,
    Faible
}
