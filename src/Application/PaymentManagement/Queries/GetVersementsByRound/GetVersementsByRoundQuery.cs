namespace Application.PaymentManagement.Queries.GetVersementsByRound;

using Application.Common;

public sealed record GetVersementsByRoundQuery(
    Guid TontineId,
    Guid RoundId) : IQuery<IReadOnlyList<VersementDto>>;
