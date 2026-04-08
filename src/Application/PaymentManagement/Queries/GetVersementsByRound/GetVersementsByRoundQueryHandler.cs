namespace Application.PaymentManagement.Queries.GetVersementsByRound;

using Application.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class GetVersementsByRoundQueryHandler
    : IQueryHandler<GetVersementsByRoundQuery, IReadOnlyList<VersementDto>>
{
    private readonly IVersementRepository _versementRepository;

    public GetVersementsByRoundQueryHandler(IVersementRepository versementRepository)
    {
        _versementRepository = versementRepository;
    }

    public async Task<IReadOnlyList<VersementDto>> Handle(
        GetVersementsByRoundQuery request,
        CancellationToken cancellationToken)
    {
        var versements = await _versementRepository.GetByTontineAndTourAsync(
            TontineId.From(request.TontineId),
            TourId.From(request.TourId),
            cancellationToken);

        return versements.Select(v => new VersementDto(
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
