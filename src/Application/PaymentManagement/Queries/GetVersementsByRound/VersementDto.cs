namespace Application.PaymentManagement.Queries.GetVersementsByRound;

public sealed record VersementDto(
    Guid Id,
    Guid TontineId,
    Guid PayeurId,
    Guid TourId,
    decimal Montant,
    string Devise,
    string Statut,
    string? ReferenceExterne,
    DateTime CreatedAt,
    DateTime? ConfirmedAt);
