namespace Application.TontineManagement.Queries.GetTontineById;

public sealed record TontineDto(
    Guid Id,
    string Name,
    string? Description,
    decimal ContributionAmount,
    string Currency,
    string Periodicity,
    string Status,
    int MaxMembers,
    int CurrentMemberCount,
    DateTime CreatedAt);
