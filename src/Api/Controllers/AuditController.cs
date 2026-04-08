namespace Api.Controllers;

using Application.PaymentManagement.Queries.VerifierAudit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoint for verifying the audit trail integrity of tontine payments.
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
    /// Verify the integrity of the audit trail for all versements of a tontine.
    /// Checks the SHA-256 hash chain for tampering.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit verification result with per-versement details.</returns>
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
