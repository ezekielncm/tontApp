namespace Infrastructure.Persistence.Repositories;

using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class UtilisateurRepository : IUtilisateurRepository
{
    private readonly TontineDbContext _dbContext;

    public UtilisateurRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Utilisateur?> GetByIdAsync(UtilisateurId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Utilisateurs
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Utilisateur?> GetByTelephoneAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var telephoneId = TelephoneId.From(telephone);
        return await _dbContext.Utilisateurs
            .FirstOrDefaultAsync(u => u.Telephone == telephoneId, cancellationToken);
    }

    public async Task<bool> ExistsByTelephoneAsync(string telephone, CancellationToken cancellationToken = default)
    {
        var telephoneId = TelephoneId.From(telephone);
        return await _dbContext.Utilisateurs
            .AnyAsync(u => u.Telephone == telephoneId, cancellationToken);
    }

    public async Task AddAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default)
    {
        await _dbContext.Utilisateurs.AddAsync(utilisateur, cancellationToken);
    }

    public Task UpdateAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default)
    {
        _dbContext.Utilisateurs.Update(utilisateur);
        return Task.CompletedTask;
    }
}
