namespace Application.CreditScoringManagement.Queries.GetProfilCredit;

using Application.Common;
using Domain.CreditScoringManagement.Repositories;

public sealed class GetProfilCreditQueryHandler
    : IQueryHandler<GetProfilCreditQuery, ProfilCreditDto?>
{
    private readonly IProfilCreditRepository _profilCreditRepository;

    public GetProfilCreditQueryHandler(IProfilCreditRepository profilCreditRepository)
    {
        _profilCreditRepository = profilCreditRepository;
    }

    public async Task<ProfilCreditDto?> Handle(
        GetProfilCreditQuery request,
        CancellationToken cancellationToken)
    {
        var profil = await _profilCreditRepository.GetByMembreIdAsync(request.MembreId, cancellationToken);

        if (profil is null)
            return null;

        var score = profil.ScoreActuel;

        var contributionCycles = Math.Min(score.CyclesCompletes * 20, 100);
        var contributionPonctualite = (int)(score.TauxPonctualite * 50);
        var contributionAnciennete = Math.Min(score.AncienneteEnMois, 24);

        return new ProfilCreditDto(
            profil.MembreId,
            score.Valeur,
            score.Niveau.ToString(),
            profil.DonneesInsuffisantes,
            new ComposantesScoreDto(
                score.CyclesCompletes,
                score.TauxPonctualite,
                score.AncienneteEnMois,
                contributionCycles,
                contributionPonctualite,
                contributionAnciennete),
            score.CalculeLe);
    }
}
