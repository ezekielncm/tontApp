namespace Domain.CreditScoringManagement.Ports;

using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.ValueObjects;

/// <summary>
/// Port for credit scoring engine.
/// V1: rule-based scoring. V2: ML-based scoring.
/// </summary>
public interface IScoringEngine
{
    /// <summary>
    /// Computes the credit score based on behavioral history.
    /// </summary>
    ScoreCalcule Calculer(HistoriqueComportement historique);
}
