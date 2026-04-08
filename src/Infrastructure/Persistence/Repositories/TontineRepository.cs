namespace Infrastructure.Persistence.Repositories;

using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class TontineRepository : ITontineRepository
{
    private readonly TontineDbContext _dbContext;

    public TontineRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Tontine?> GetByIdAsync(TontineId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
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
