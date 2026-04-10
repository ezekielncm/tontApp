namespace Application.TontineManagement.Queries.GetMesTontines;

using Application.Common;
using Application.TontineManagement.Queries.GetTontineById;
using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement.Repositories;

public sealed class GetMesTontinesQueryHandler
    : IQueryHandler<GetMesTontinesQuery, IReadOnlyList<TontineDto>>
{
    private readonly ITontineRepository _tontineRepository;

    public GetMesTontinesQueryHandler(ITontineRepository tontineRepository)
    {
        _tontineRepository = tontineRepository;
    }

    public async Task<IReadOnlyList<TontineDto>> Handle(
        GetMesTontinesQuery request,
        CancellationToken cancellationToken)
    {
        var gestionnaireId = UtilisateurId.From(request.GestionnaireId);
        var tontines = await _tontineRepository.GetByGestionnaireIdReadOnlyAsync(
            gestionnaireId, cancellationToken);

        return tontines.Select(t => new TontineDto(
            t.Id.Value,
            t.Name,
            t.Description,
            t.ContributionAmount.Amount,
            t.ContributionAmount.Currency,
            t.Periodicity.ToString(),
            t.Status.ToString(),
            t.MaxMembers,
            t.Members.Count,
            t.GestionnaireId.Value,
            t.CreatedAt)).ToList();
    }
}
