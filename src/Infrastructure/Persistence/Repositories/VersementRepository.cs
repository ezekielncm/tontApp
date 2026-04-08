namespace Infrastructure.Persistence.Repositories;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class VersementRepository : IVersementRepository
{
    private readonly TontineDbContext _dbContext;

    public VersementRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Versement?> GetByIdAsync(VersementId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Versement>> GetByTontineAndRoundAsync(
        TontineId tontineId, RoundId roundId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Where(v => v.TontineId == tontineId && v.RoundId == roundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Versement>> GetByMemberAsync(
        MemberId memberId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Versements
            .Where(v => v.MemberId == memberId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Versement versement, CancellationToken cancellationToken = default)
    {
        await _dbContext.Versements.AddAsync(versement, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Versement versement, CancellationToken cancellationToken = default)
    {
        _dbContext.Versements.Update(versement);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
