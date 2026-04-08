namespace Api.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.BillingManagement.Commands.SouscrireAbonnement;
using Application.BillingManagement.Queries.GetAbonnementByGestionnaire;
using Application.BillingManagement.Queries.GetPlans;
using Infrastructure.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoints for subscription billing: plans, subscription, and current subscription details.
/// </summary>
[ApiController]
[Route("api/v1/billing")]
[Produces("application/json")]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List all available subscription plans.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available plans with pricing and limits.</returns>
    /// <response code="200">Plans returned successfully.</response>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await _mediator.Send(new GetPlansQuery(), cancellationToken);
        return Ok(plans);
    }

    /// <summary>
    /// Subscribe to a plan. Initiates Orange Money payment for paid plans.
    /// </summary>
    /// <param name="request">Subscription request with plan code and phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Subscription details and payment status.</returns>
    /// <response code="201">Subscription created successfully.</response>
    /// <response code="400">Validation error or payment failure.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("souscrire")]
    [Authorize]
    [ProducesResponseType(typeof(SouscrireAbonnementResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Souscrire(
        [FromBody] SouscrireAbonnementRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Identité utilisateur non déterminée." });

        try
        {
            var command = new SouscrireAbonnementCommand(
                userId,
                request.PlanCode,
                request.NumeroTelephone);

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(MonAbonnement), null, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current user's subscription details.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current subscription details or 404 if none.</returns>
    /// <response code="200">Subscription found.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No subscription found for this user.</response>
    [HttpGet("mon-abonnement")]
    [Authorize]
    [ProducesResponseType(typeof(AbonnementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MonAbonnement(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Identité utilisateur non déterminée." });

        var query = new GetAbonnementByGestionnaireQuery(userId);
        var abonnement = await _mediator.Send(query, cancellationToken);

        if (abonnement is null)
            return NotFound(new { error = "Aucun abonnement trouvé." });

        return Ok(abonnement);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// Request body for subscribing to a plan.
/// </summary>
public sealed record SouscrireAbonnementRequest(
    string PlanCode,
    string NumeroTelephone);
