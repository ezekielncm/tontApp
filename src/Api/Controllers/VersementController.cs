namespace Api.Controllers;

using Application.Common;
using Application.PaymentManagement.Commands.EnregistrerVersementManuel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoints for managing versements (payments).
/// </summary>
[ApiController]
[Route("api/v1/versements")]
[Produces("application/json")]
[Authorize]
public class VersementController : ControllerBase
{
    private readonly IMediator _mediator;

    public VersementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a manual cash payment made by the gestionnaire on behalf of a member.
    /// The versement is created with CONFIRME status directly (no mobile money flow).
    /// </summary>
    /// <param name="request">Manual payment details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    /// <response code="200">Manual payment registered successfully.</response>
    /// <response code="400">Validation error or business rule violation.</response>
    /// <response code="403">User does not have the GESTIONNAIRE role.</response>
    [HttpPost("manuel")]
    [Authorize(Roles = "GESTIONNAIRE")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnregistrerVersementManuel(
        [FromBody] EnregistrerVersementManuelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EnregistrerVersementManuelCommand(
            request.TontineId,
            request.TourId,
            request.MembreId,
            request.Montant,
            request.DescriptionPreuve);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { status = "versement_manuel_enregistre" });
    }
}

/// <summary>
/// Request body for registering a manual cash payment.
/// </summary>
public sealed record EnregistrerVersementManuelRequest(
    Guid TontineId,
    Guid TourId,
    Guid MembreId,
    decimal Montant,
    string DescriptionPreuve);
