namespace Application.PaymentManagement.Queries.GetMesVersements;

using Application.Common;
using Application.PaymentManagement.Queries.GetVersementsByRound;

public sealed record GetMesVersementsQuery(
    Guid PayeurId,
    Guid? TontineId = null) : IQuery<IReadOnlyList<VersementDto>>;
