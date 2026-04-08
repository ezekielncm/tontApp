namespace Application.NotificationManagement.Commands.SendNotification;

using Application.Common;

public sealed record SendNotificationCommand(Guid NotificationId) : ICommand;
