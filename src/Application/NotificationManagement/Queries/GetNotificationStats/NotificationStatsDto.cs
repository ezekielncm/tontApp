namespace Application.NotificationManagement.Queries.GetNotificationStats;

public sealed record NotificationStatsDto(
    int TotalEnvoyees,
    int TotalEchouees,
    int TotalEnAttente,
    double TauxEchec,
    IReadOnlyList<NotificationTypeStatDto> ParType);

public sealed record NotificationTypeStatDto(
    string Type,
    int Count);
