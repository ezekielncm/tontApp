namespace Application.NotificationManagement.Queries.GetPendingNotifications;

public sealed record NotificationDto(
    Guid Id,
    string DestinataireId,
    string Type,
    string Contenu,
    string Statut,
    int TentativesEnvoi,
    int MaxTentatives,
    DateTime CreatedAt,
    DateTime? SentAt);
