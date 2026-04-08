namespace Application.PaymentManagement.Queries.GetVersementsByRound;

using Application.Common;
using Domain.PaymentManagement.Repositories;
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
        var versements = await _versementRepository.GetByTontineAndRoundAsync(
            TontineId.From(request.TontineId),
            RoundId.From(request.RoundId),
            cancellationToken);

        return versements.Select(v => new VersementDto(
            v.Id.Value,
            v.TontineId.Value,
            v.MemberId.Value,
            v.RoundId.Value,
            v.Montant,
            v.Currency,
            v.Statut.ToString(),
            v.ReferenceExterne,
            v.CreatedAt,
            v.ConfirmedAt)).ToList();
    }
}
