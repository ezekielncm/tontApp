namespace Application.CreditScoringManagement.Queries.GetProfilCredit;

using Application.Common;

public sealed record GetProfilCreditQuery(Guid MembreId) : IQuery<ProfilCreditDto?>;

public sealed record ProfilCreditDto(
    Guid MembreId,
    int Score,
    string Niveau,
    bool DonneesInsuffisantes,
    ComposantesScoreDto Composantes,
    DateTime CalculeLe);

public sealed record ComposantesScoreDto(
    int CyclesCompletes,
    double TauxPonctualite,
    int AncienneteEnMois,
    int ContributionCycles,
    int ContributionPonctualite,
    int ContributionAnciennete);
