namespace Infrastructure.Persistence.Repositories;

using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class AbonnementRepository : IAbonnementRepository
{
    private readonly TontineDbContext _dbContext;

    public AbonnementRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Abonnement?> GetByIdAsync(AbonnementId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Abonnements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Abonnement?> GetByGestionnaireAsync(
        string gestionnaireId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Abonnements
            .FirstOrDefaultAsync(a => a.GestionnaireId == gestionnaireId, cancellationToken);
    }

    public async Task AddAsync(Abonnement abonnement, CancellationToken cancellationToken = default)
    {
        await _dbContext.Abonnements.AddAsync(abonnement, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Abonnement abonnement, CancellationToken cancellationToken = default)
    {
        _dbContext.Abonnements.Update(abonnement);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
