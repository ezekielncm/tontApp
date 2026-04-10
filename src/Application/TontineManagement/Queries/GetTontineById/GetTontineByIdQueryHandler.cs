namespace Application.TontineManagement.Queries.GetTontineById;

using Application.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class GetTontineByIdQueryHandler : IQueryHandler<GetTontineByIdQuery, TontineDto?>
{
    private readonly ITontineRepository _tontineRepository;

    public GetTontineByIdQueryHandler(ITontineRepository tontineRepository)
    {
        _tontineRepository = tontineRepository;
    }

    public async Task<TontineDto?> Handle(GetTontineByIdQuery request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken);

        if (tontine is null)
            return null;

        return new TontineDto(
            tontine.Id.Value,
            tontine.Name,
            tontine.Description,
            tontine.ContributionAmount.Amount,
            tontine.ContributionAmount.Currency,
            tontine.Periodicity.ToString(),
            tontine.Status.ToString(),
            tontine.MaxMembers,
            tontine.Members.Count,
            tontine.GestionnaireId.Value,
            tontine.CreatedAt);
    }
}
