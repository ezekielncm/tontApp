namespace Application.PaymentManagement.Queries.GetAuditEntries;

using Application.Common;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class GetAuditEntriesQueryHandler
    : IQueryHandler<GetAuditEntriesQuery, AuditEntriesResult>
{
    private readonly IAuditEntryRepository _auditEntryRepository;

    public GetAuditEntriesQueryHandler(IAuditEntryRepository auditEntryRepository)
    {
        _auditEntryRepository = auditEntryRepository;
    }

    public async Task<AuditEntriesResult> Handle(
        GetAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var tontineId = TontineId.From(request.TontineId);

        var entries = await _auditEntryRepository.GetByTontinePagedAsync(
            tontineId, request.Page, request.PageSize, cancellationToken);

        var totalCount = await _auditEntryRepository.CountByTontineAsync(
            tontineId, cancellationToken);

        var dtos = entries.Select(e => new AuditEntryDto(
            e.Id.Value,
            e.VersementId.Value,
            e.TontineId.Value,
            e.Action.ToString(),
            e.ActeurId,
            e.Timestamp,
            e.Payload,
            e.HashPrecedent,
            e.HashCourant)).ToList();

        return new AuditEntriesResult(dtos, totalCount, request.Page, request.PageSize);
    }
}
