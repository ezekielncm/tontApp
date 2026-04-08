namespace Api.Controllers;

using Application.PaymentManagement.Queries.GetAuditEntries;
using Application.PaymentManagement.Queries.VerifierAudit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoints for the audit trail: paginated listing and chain integrity verification.
/// </summary>
[ApiController]
[Route("api/v1/tontines")]
[Produces("application/json")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated audit entries for a tontine (default: last 50 entries).
    /// Entries are ordered by timestamp descending (newest first).
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Number of entries per page (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit entries.</returns>
    /// <response code="200">Audit entries returned.</response>
    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(AuditEntriesResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditEntries(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = new GetAuditEntriesQuery(id, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Verify the integrity of the audit trail for all versements of a tontine.
    /// Checks the SHA-256 hash chain for tampering.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit verification result with chain integrity report.</returns>
    /// <response code="200">Audit verification completed.</response>
    [HttpGet("{id:guid}/audit/verifier")]
    [ProducesResponseType(typeof(AuditVerificationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifierAudit(Guid id, CancellationToken cancellationToken)
    {
        var query = new VerifierAuditQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
