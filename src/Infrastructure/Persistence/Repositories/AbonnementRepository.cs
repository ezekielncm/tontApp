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
            .Where(a => a.GestionnaireId == gestionnaireId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Abonnement>> GetExpiringAsync(
        DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Abonnements
            .Where(a => a.Statut == StatutAbonnement.Actif
                && a.Plan != PlanTarifaire.Gratuit
                && a.DateFin <= beforeDate
                && a.RenouvellementAuto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Abonnement>> GetInGraceAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Abonnements
            .Where(a => a.Statut == StatutAbonnement.EnGrace
                && a.DateFinGrace.HasValue
                && a.DateFinGrace.Value <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Abonnement>> GetActiveByPlanAsync(
        PlanTarifaire plan, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Abonnements
            .Where(a => a.Plan == plan && a.Statut == StatutAbonnement.Actif)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Abonnement abonnement, CancellationToken cancellationToken = default)
    {
        await _dbContext.Abonnements.AddAsync(abonnement, cancellationToken);
    }

    public Task UpdateAsync(Abonnement abonnement, CancellationToken cancellationToken = default)
    {
        _dbContext.Abonnements.Update(abonnement);
        return Task.CompletedTask;
    }
}
