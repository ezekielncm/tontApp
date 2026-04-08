namespace Application.NotificationManagement.Commands.CreateNotification;

using Application.Common;
using Domain.NotificationManagement;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;

public sealed class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, Guid>
{
    private readonly INotificationRepository _notificationRepository;

    public CreateNotificationCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var type = Enum.Parse<NotificationType>(request.Type, ignoreCase: true);

        var notification = Notification.Create(
            request.DestinataireId,
            type,
            request.Contenu,
            request.MaxTentatives);

        await _notificationRepository.AddAsync(notification, cancellationToken);

        return notification.Id.Value;
    }
}
