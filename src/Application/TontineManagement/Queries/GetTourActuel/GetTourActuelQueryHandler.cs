namespace Application.TontineManagement.Queries.GetTourActuel;

using Application.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class GetTourActuelQueryHandler : IQueryHandler<GetTourActuelQuery, TourActuelDto?>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IVersementRepository _versementRepository;

    public GetTourActuelQueryHandler(
        ITontineRepository tontineRepository,
        IVersementRepository versementRepository)
    {
        _tontineRepository = tontineRepository;
        _versementRepository = versementRepository;
    }

    public async Task<TourActuelDto?> Handle(GetTourActuelQuery request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdReadOnlyAsync(
            TontineId.From(request.TontineId), cancellationToken);

        if (tontine is null)
            return null;

        var currentRound = tontine.Rounds.FirstOrDefault(r => !r.IsCompleted);
        if (currentRound is null)
            return null;

        var beneficiary = tontine.Members.FirstOrDefault(m => m.Id == currentRound.BeneficiaryId);

        var versements = await _versementRepository.GetByTontineAndTourAsync(
            tontine.Id, TourId.From(currentRound.Id.Value), cancellationToken);

        var activeMembers = tontine.Members
            .Where(m => m.Statut == StatutMembre.Actif)
            .ToList();

        var montantAttendu = tontine.ContributionAmount.Amount * activeMembers.Count;
        var montantCollecte = versements
            .Where(v => v.Statut == VersementStatus.Confirme)
            .Sum(v => v.Montant.Valeur);

        var paidMemberIds = versements
            .Where(v => v.Statut == VersementStatus.Confirme)
            .Select(v => v.PayeurId.Value)
            .ToHashSet();

        var payeurs = activeMembers.Select(m => new PayeurDto(
            m.Id.Value,
            m.Name,
            paidMemberIds.Contains(m.Id.Value) ? "Payé" : "En attente"
        )).ToList();

        var pourcentage = montantAttendu > 0
            ? (double)(montantCollecte / montantAttendu) * 100
            : 0;

        return new TourActuelDto(
            currentRound.Id.Value,
            currentRound.RoundNumber,
            beneficiary?.Name ?? "Inconnu",
            beneficiary?.Id.Value ?? Guid.Empty,
            currentRound.ScheduledDate,
            currentRound.DateLimite,
            montantCollecte,
            montantAttendu,
            paidMemberIds.Count,
            activeMembers.Count,
            Math.Round(pourcentage, 1),
            payeurs);
    }
}
