namespace Infrastructure.Persistence.Repositories;

using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class TontineRepository : ITontineRepository
{
    private const string MembersNavigation = "_members";
    private const string RoundsNavigation = "_rounds";
    private const string InvitationsNavigation = "_invitations";

    private readonly TontineDbContext _dbContext;

    public TontineRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Tontine?> GetByIdAsync(TontineId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .Include(InvitationsNavigation)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tontine?> GetByIdReadOnlyAsync(TontineId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .AsNoTracking()
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .Include(InvitationsNavigation)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Tontine>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .AsNoTracking()
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tontine>> GetByStatusReadOnlyAsync(TontineStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .AsNoTracking()
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .Where(t => t.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tontine tontine, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tontines.AddAsync(tontine, cancellationToken);
    }

    public Task UpdateAsync(Tontine tontine, CancellationToken cancellationToken = default)
    {
        _dbContext.Tontines.Update(tontine);
        return Task.CompletedTask;
    }
}
