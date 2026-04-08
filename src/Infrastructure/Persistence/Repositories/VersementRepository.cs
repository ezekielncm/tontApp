namespace Infrastructure.Persistence.Repositories;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class VersementRepository : IVersementRepository
{
    private const string AuditTrailNavigation = "_auditTrail";
    private readonly TontineDbContext _dbContext;

    public VersementRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Versement?> GetByIdAsync(VersementId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Include(AuditTrailNavigation)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Versement?> GetByReferenceExterneAsync(string referenceExterne, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Include(AuditTrailNavigation)
            .FirstOrDefaultAsync(v => v.ReferenceExterne == referenceExterne, cancellationToken);
    }

    public async Task<IReadOnlyList<Versement>> GetByTontineAndTourAsync(
        TontineId tontineId, TourId tourId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Where(v => v.TontineId == tontineId && v.TourId == tourId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Versement>> GetByTontineAsync(
        TontineId tontineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Include(AuditTrailNavigation)
            .Where(v => v.TontineId == tontineId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Versement>> GetByPayeurAsync(
        PayeurId payeurId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Where(v => v.PayeurId == payeurId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Versement?> GetLastByTontineAsync(
        TontineId tontineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Where(v => v.TontineId == tontineId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Versement versement, CancellationToken cancellationToken = default)
    {
        await _dbContext.Versements.AddAsync(versement, cancellationToken);
    }

    public Task UpdateAsync(Versement versement, CancellationToken cancellationToken = default)
    {
        _dbContext.Versements.Update(versement);
        return Task.CompletedTask;
    }
}
