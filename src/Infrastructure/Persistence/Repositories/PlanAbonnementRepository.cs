namespace Infrastructure.Persistence.Repositories;

using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class PlanAbonnementRepository : IPlanAbonnementRepository
{
    private readonly TontineDbContext _dbContext;

    public PlanAbonnementRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlanAbonnement?> GetByIdAsync(
        PlanAbonnementId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlansAbonnement
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PlanAbonnement?> GetByCodeAsync(
        string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlansAbonnement
            .FirstOrDefaultAsync(p => p.Code == code && p.EstActif, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanAbonnement>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlansAbonnement
            .Where(p => p.EstActif)
            .OrderBy(p => p.PrixMensuel)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlanAbonnement plan, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlansAbonnement.AddAsync(plan, cancellationToken);
    }
}
