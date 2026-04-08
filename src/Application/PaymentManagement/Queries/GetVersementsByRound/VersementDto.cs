namespace Application.PaymentManagement.Queries.GetVersementsByRound;

public sealed record VersementDto(
    Guid Id,
    Guid TontineId,
    Guid MemberId,
    Guid RoundId,
    decimal Montant,
    string Currency,
    string Statut,
    string? ReferenceExterne,
    DateTime CreatedAt,
    DateTime? ConfirmedAt);
