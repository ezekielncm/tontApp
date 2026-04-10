namespace Domain.NotificationManagement.Repositories;

using Domain.NotificationManagement.ValueObjects;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts SMS notifications sent to a member today for rate limiting (max 10 SMS/member/day).
    /// </summary>
    Task<int> CountTodayByDestinataireAsync(string destinataireId, CancellationToken cancellationToken = default);
}
