namespace Infrastructure.Persistence.Repositories;

using Domain.CreditScoringManagement;
using Domain.CreditScoringManagement.Repositories;
using Domain.CreditScoringManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class ProfilCreditRepository : IProfilCreditRepository
{
    private readonly TontineDbContext _dbContext;

    public ProfilCreditRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfilCredit?> GetByIdAsync(ProfilCreditId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProfilsCredit
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProfilCredit?> GetByMembreIdAsync(Guid membreId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProfilsCredit
            .FirstOrDefaultAsync(p => p.MembreId == membreId, cancellationToken);
    }

    public async Task AddAsync(ProfilCredit profilCredit, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProfilsCredit.AddAsync(profilCredit, cancellationToken);
    }

    public Task UpdateAsync(ProfilCredit profilCredit, CancellationToken cancellationToken = default)
    {
        _dbContext.ProfilsCredit.Update(profilCredit);
        return Task.CompletedTask;
    }
}
