namespace PaymentIntegrationTests;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Test repository implementation using PaymentTestDbContext.
/// Mirrors the production VersementRepository logic.
/// </summary>
internal sealed class TestVersementRepository : IVersementRepository
{
    private const string AuditTrailNavigation = "AuditTrail";
    private readonly PaymentTestDbContext _dbContext;

    public TestVersementRepository(PaymentTestDbContext dbContext)
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

/// <summary>
/// Test repository implementation using PaymentTestDbContext.
/// Mirrors the production AuditEntryRepository logic.
/// </summary>
internal sealed class TestAuditEntryRepository : IAuditEntryRepository
{
    private readonly PaymentTestDbContext _dbContext;

    public TestAuditEntryRepository(PaymentTestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByTontinePagedAsync(
        TontineId tontineId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.TontineId == tontineId)
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByTontineOrderedAsync(
        TontineId tontineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.TontineId == tontineId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditEntry?> GetLastByTontineAsync(
        TontineId tontineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.TontineId == tontineId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountByTontineAsync(
        TontineId tontineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditEntries
            .Where(e => e.TontineId == tontineId)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditEntries.AddAsync(entry, cancellationToken);
    }
}
