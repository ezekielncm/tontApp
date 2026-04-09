namespace Infrastructure.CreditScoring;

using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.Ports;
using Domain.CreditScoringManagement.ValueObjects;

/// <summary>
/// Rule-based scoring engine (v1).
/// Formula: score = (cyclesCompletes × 20) + (tauxPonctualite × 50) + min(ancienneteEnMois, 24)
/// Score is clamped between 0 and 100.
/// </summary>
public sealed class RegleMetierScoringEngine : IScoringEngine
{
    public ScoreCalcule Calculer(HistoriqueComportement historique)
    {
        return ScoreCalcule.Create(
            historique.CyclesCompletes,
            historique.CalculerTauxPonctualite(),
            historique.CalculerAncienneteEnMois());
    }
}
