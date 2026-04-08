namespace Application.BillingManagement.Queries.GetPlans;

using Application.Common;
using Domain.BillingManagement.Repositories;

public sealed class GetPlansQueryHandler
    : IQueryHandler<GetPlansQuery, IReadOnlyList<PlanDto>>
{
    private readonly IPlanAbonnementRepository _planRepository;

    public GetPlansQueryHandler(IPlanAbonnementRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<PlanDto>> Handle(
        GetPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await _planRepository.GetAllActiveAsync(cancellationToken);

        return plans.Select(p => new PlanDto(
            p.Id.Value,
            p.Nom,
            p.Code,
            p.PrixMensuel,
            p.Devise,
            p.MaxTontines,
            p.MaxMembresParTontine,
            p.Description)).ToList();
    }
}
