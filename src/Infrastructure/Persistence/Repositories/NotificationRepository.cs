namespace Infrastructure.Persistence.Repositories;

using Domain.NotificationManagement;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly TontineDbContext _dbContext;

    public NotificationRepository(TontineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.Statut == NotificationStatus.EnAttente)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<int> CountTodayByDestinataireAsync(string destinataireId, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _dbContext.Notifications
            .Where(n => n.DestinataireId == destinataireId && n.CreatedAt >= todayUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> CountTodayByDestinatairesAsync(IEnumerable<string> destinataireIds, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var ids = destinataireIds.ToList();

        var counts = await _dbContext.Notifications
            .Where(n => ids.Contains(n.DestinataireId) && n.CreatedAt >= todayUtc)
            .GroupBy(n => n.DestinataireId)
            .Select(g => new { DestinataireId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.DestinataireId, v => v.Count, cancellationToken);

        // Ensure all provided IDs have an entry, even if 0
        foreach (var id in ids)
        {
            if (!counts.ContainsKey(id))
            {
                counts[id] = 0;
            }
        }

        return counts;
    }
}
