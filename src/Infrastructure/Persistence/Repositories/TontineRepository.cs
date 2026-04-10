namespace Infrastructure.Persistence.Repositories;

using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class TontineRepository : ITontineRepository
{
    private const string MembersNavigation = "Members";
    private const string RoundsNavigation = "Rounds";
    private const string InvitationsNavigation = "Invitations";

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

    public async Task<Tontine?> GetByInvitationCodeHashAsync(string codeHash, CancellationToken cancellationToken = default)
    {
        // Query tontines that have an invitation with the matching code hash
        return await _dbContext.Tontines
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .Include(InvitationsNavigation)
            .Where(t => _dbContext.Set<Invitation>()
                .Any(i => EF.Property<Guid>(i, "tontine_id") == t.Id.Value && i.CodeHash == codeHash))
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task<int> CountByGestionnaireAsync(string gestionnaireId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(gestionnaireId, out var guid))
            return 0;

        return await _dbContext.Tontines
            .Where(t => t.GestionnaireId == UtilisateurId.From(guid)
                      && t.Status != TontineStatus.Cancelled)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tontine>> GetByGestionnaireIdReadOnlyAsync(UtilisateurId gestionnaireId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tontines
            .AsNoTracking()
            .Include(MembersNavigation)
            .Include(RoundsNavigation)
            .Where(t => t.GestionnaireId == gestionnaireId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
