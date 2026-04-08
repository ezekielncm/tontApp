namespace Application.PaymentManagement.Queries.GetAuditEntries;

using Application.Common;

public sealed record GetAuditEntriesQuery(Guid TontineId, int Page = 1, int PageSize = 50)
    : IQuery<AuditEntriesResult>;
