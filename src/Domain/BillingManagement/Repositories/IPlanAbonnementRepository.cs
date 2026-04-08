namespace Domain.BillingManagement.Repositories;

using Domain.BillingManagement.ValueObjects;

public interface IPlanAbonnementRepository
{
    Task<PlanAbonnement?> GetByIdAsync(PlanAbonnementId id, CancellationToken cancellationToken = default);
    Task<PlanAbonnement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanAbonnement>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PlanAbonnement plan, CancellationToken cancellationToken = default);
}
