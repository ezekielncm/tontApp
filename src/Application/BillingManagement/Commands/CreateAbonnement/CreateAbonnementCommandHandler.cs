namespace Application.BillingManagement.Commands.CreateAbonnement;

using Application.BillingManagement.Services;
using Application.Common;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class CreateAbonnementCommandHandler : ICommandHandler<CreateAbonnementCommand, Guid>
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IPlanAbonnementRepository _planRepository;
    private readonly IBillingCacheService _billingCache;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAbonnementCommandHandler(
        IAbonnementRepository abonnementRepository,
        IPlanAbonnementRepository planRepository,
        IBillingCacheService billingCache,
        IUnitOfWork unitOfWork)
    {
        _abonnementRepository = abonnementRepository;
        _planRepository = planRepository;
        _billingCache = billingCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAbonnementCommand request, CancellationToken cancellationToken)
    {
        var planTarifaire = Enum.Parse<PlanTarifaire>(request.Plan, ignoreCase: true);

        var abonnement = Abonnement.Create(request.GestionnaireId, planTarifaire);

        await _abonnementRepository.AddAsync(abonnement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update Redis cache with plan limits
        var planCode = planTarifaire.ToString().ToUpperInvariant();
        var plan = await _planRepository.GetByCodeAsync(planCode, cancellationToken);
        if (plan is not null)
        {
            await _billingCache.SetPlanLimitsAsync(
                request.GestionnaireId,
                plan.MaxTontines,
                plan.MaxMembresParTontine,
                cancellationToken);
        }

        return abonnement.Id.Value;
    }
}
