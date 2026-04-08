namespace Application.PaymentManagement.Services;

using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public sealed class AuditTrailService : IAuditTrailService
{
    private readonly IAuditEntryRepository _auditEntryRepository;

    public AuditTrailService(IAuditEntryRepository auditEntryRepository)
    {
        _auditEntryRepository = auditEntryRepository;
    }

    public async Task<AuditEntry> AjouterEntree(
        TontineId tontineId,
        VersementId versementId,
        AuditAction action,
        string acteurId,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var dernierHash = await GetDernierHash(tontineId, cancellationToken);

        var entry = AuditEntry.Create(tontineId, versementId, action, acteurId, payload, dernierHash);
        await _auditEntryRepository.AddAsync(entry, cancellationToken);

        return entry;
    }

    public async Task<ChainVerificationReport> VerifierChaine(
        TontineId tontineId,
        CancellationToken cancellationToken = default)
    {
        var entries = await _auditEntryRepository.GetByTontineOrderedAsync(tontineId, cancellationToken);

        var nombreValides = 0;
        var nombreInvalides = 0;
        string? premiereInvalideId = null;

        var hashPrecedent = AuditEntry.GenesisHash;

        foreach (var entry in entries)
        {
            if (entry.VerifyIntegrity(hashPrecedent))
            {
                nombreValides++;
            }
            else
            {
                nombreInvalides++;
                premiereInvalideId ??= entry.Id.Value.ToString();
            }

            hashPrecedent = entry.HashCourant;
        }

        var dernierHash = entries.Count > 0
            ? entries[^1].HashCourant
            : AuditEntry.GenesisHash;

        return new ChainVerificationReport(
            EstIntegre: nombreInvalides == 0,
            NombreEntrees: entries.Count,
            NombreEntreesValides: nombreValides,
            NombreEntreesInvalides: nombreInvalides,
            VerificationTimestamp: DateTime.UtcNow,
            PremiereEntreeInvalideId: premiereInvalideId,
            DernierHash: dernierHash);
    }

    public async Task<string> GetDernierHash(
        TontineId tontineId,
        CancellationToken cancellationToken = default)
    {
        var lastEntry = await _auditEntryRepository.GetLastByTontineAsync(tontineId, cancellationToken);
        return lastEntry?.HashCourant ?? AuditEntry.GenesisHash;
    }
}
