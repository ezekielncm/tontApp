namespace Application.NotificationManagement.Commands.SendNotification;

using Application.Common;
using Domain.Common;
using Domain.NotificationManagement.Repositories;
using Domain.NotificationManagement.ValueObjects;

public sealed class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendNotificationCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(
            NotificationId.From(request.NotificationId), cancellationToken)
            ?? throw new InvalidOperationException($"Notification {request.NotificationId} not found.");

        // In a real implementation, this would delegate to an infrastructure SMS/notification service
        notification.MarquerEnvoyee();

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
