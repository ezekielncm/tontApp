namespace Infrastructure.Jobs;

using Application.PaymentManagement.Services;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily Hangfire job that verifies the audit chain integrity for all active tontines.
/// Alerts (via logging) if any chain is compromised.
/// </summary>
public sealed class VerifierChaineAuditJob
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ILogger<VerifierChaineAuditJob> _logger;

    public VerifierChaineAuditJob(
        ITontineRepository tontineRepository,
        IAuditTrailService auditTrailService,
        ILogger<VerifierChaineAuditJob> logger)
    {
        _tontineRepository = tontineRepository;
        _auditTrailService = auditTrailService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the daily audit chain verification across all active tontines.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting daily audit chain verification...");

        var tontines = await _tontineRepository.GetByStatusReadOnlyAsync(
            TontineStatus.Active, cancellationToken);

        var totalTontines = tontines.Count;
        var tontinesCompromises = 0;
        var totalEntrees = 0;
        var totalEntreesValides = 0;

        foreach (var tontine in tontines)
        {
            var report = await _auditTrailService.VerifierChaine(tontine.Id, cancellationToken);
            totalEntrees += report.NombreEntrees;
            totalEntreesValides += report.NombreEntreesValides;

            if (!report.EstIntegre)
            {
                tontinesCompromises++;

                _logger.LogCritical(
                    "AUDIT CHAIN COMPROMISED for tontine {TontineId}: " +
                    "{NombreEntreesInvalides}/{NombreEntrees} entries invalid. " +
                    "First invalid entry: {PremiereEntreeInvalideId}",
                    tontine.Id.Value,
                    report.NombreEntreesInvalides,
                    report.NombreEntrees,
                    report.PremiereEntreeInvalideId);
            }
            else
            {
                _logger.LogDebug(
                    "Audit chain OK for tontine {TontineId}: {NombreEntrees} entries verified",
                    tontine.Id.Value,
                    report.NombreEntrees);
            }
        }

        if (tontinesCompromises > 0)
        {
            _logger.LogCritical(
                "Daily audit verification FAILED: {TontinesCompromises}/{TotalTontines} tontines compromised. " +
                "Total entries: {TotalEntrees}, Valid: {TotalEntreesValides}",
                tontinesCompromises,
                totalTontines,
                totalEntrees,
                totalEntreesValides);
        }
        else
        {
            _logger.LogInformation(
                "Daily audit verification PASSED: {TotalTontines} tontines checked, " +
                "{TotalEntrees} entries verified, all chains intact. Last verification: {Timestamp:O}",
                totalTontines,
                totalEntrees,
                DateTime.UtcNow);
        }
    }
}
