namespace Application.PaymentManagement.Services;

using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

/// <summary>
/// Service for managing the tamper-evident audit trail chain.
/// Each tontine has its own SHA-256 hash chain of audit entries.
/// </summary>
public interface IAuditTrailService
{
    /// <summary>
    /// Adds a new entry to the tontine's audit chain.
    /// Automatically chains from the last entry (or GENESIS for the first).
    /// </summary>
    Task<AuditEntry> AjouterEntree(
        TontineId tontineId,
        VersementId versementId,
        AuditAction action,
        string acteurId,
        string payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of the entire audit chain for a tontine.
    /// Returns a detailed verification report.
    /// </summary>
    Task<ChainVerificationReport> VerifierChaine(
        TontineId tontineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the hash of the last audit entry for a tontine.
    /// Returns GENESIS hash if no entries exist.
    /// </summary>
    Task<string> GetDernierHash(
        TontineId tontineId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Report produced by chain verification.
/// </summary>
public sealed record ChainVerificationReport(
    bool EstIntegre,
    int NombreEntrees,
    int NombreEntreesValides,
    int NombreEntreesInvalides,
    DateTime VerificationTimestamp,
    string? PremiereEntreeInvalideId,
    string DernierHash);
