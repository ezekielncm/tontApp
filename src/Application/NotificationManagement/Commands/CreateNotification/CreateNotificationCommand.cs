namespace Application.NotificationManagement.Commands.CreateNotification;

using Application.Common;

public sealed record CreateNotificationCommand(
    string DestinataireId,
    string Type,
    string Contenu,
    int MaxTentatives = 3) : ICommand<Guid>;
