namespace Api.Controllers;

using Application.CreditScoringManagement.Queries.GetProfilCredit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoint for member credit profiles.
/// Public to gestionnaires reviewing membership applications.
/// </summary>
[ApiController]
[Route("api/v1/membres")]
[Produces("application/json")]
public class ProfilCreditController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfilCreditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the credit profile of a member by their ID.
    /// Returns score, risk level, and score components.
    /// </summary>
    /// <param name="id">Member ID (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Credit profile with score and breakdown.</returns>
    /// <response code="200">Credit profile found.</response>
    /// <response code="404">No credit profile found for this member.</response>
    [HttpGet("{id:guid}/profil-credit")]
    [Authorize]
    [ProducesResponseType(typeof(ProfilCreditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfilCredit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProfilCreditQuery(id), cancellationToken);

        if (result is null)
            return NotFound(new { error = "Aucun profil crédit trouvé pour ce membre." });

        return Ok(result);
    }
}
