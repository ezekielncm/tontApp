namespace Application.PaymentManagement.Queries.GetMesVersements;

using Application.Common;
using Application.PaymentManagement.Queries.GetVersementsByRound;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class GetMesVersementsQueryHandler
    : IQueryHandler<GetMesVersementsQuery, IReadOnlyList<VersementDto>>
{
    private readonly IVersementRepository _versementRepository;

    public GetMesVersementsQueryHandler(IVersementRepository versementRepository)
    {
        _versementRepository = versementRepository;
    }

    public async Task<IReadOnlyList<VersementDto>> Handle(
        GetMesVersementsQuery request,
        CancellationToken cancellationToken)
    {
        var versements = await _versementRepository.GetByPayeurAsync(
            PayeurId.From(request.PayeurId), cancellationToken);

        IEnumerable<Domain.PaymentManagement.Versement> filtered = versements;

        if (request.TontineId.HasValue)
        {
            var tontineId = TontineId.From(request.TontineId.Value);
            filtered = filtered.Where(v => v.TontineId == tontineId);
        }

        return filtered.Select(v => new VersementDto(
            v.Id.Value,
            v.TontineId.Value,
            v.PayeurId.Value,
            v.TourId.Value,
            v.Montant.Valeur,
            v.Montant.Devise,
            v.Statut.ToString(),
            v.ReferenceExterne,
            v.CreatedAt,
            v.ConfirmedAt)).ToList();
    }
}
