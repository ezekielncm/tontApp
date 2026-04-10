namespace Api.Controllers;

using Application.Common;
using Application.PaymentManagement.Commands.ConfirmVersement;
using Application.PaymentManagement.Commands.EnregistrerVersementManuel;
using Application.PaymentManagement.Commands.InitierVersement;
using Application.PaymentManagement.Queries.GetAuditEntries;
using Application.PaymentManagement.Queries.GetVersementsByRound;
using Application.PaymentManagement.Queries.VerifierAudit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
    [Authorize(Roles = "Gestionnaire,Admin")]
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

    // ── POST /api/v1/versements/initier ────────────────────────────
    /// <summary>
    /// Initiate a mobile money payment for a round contribution.
    /// </summary>
    [HttpPost("initier")]
    public async Task<IActionResult> Initier(
        [FromBody] InitierVersementRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var payeurId))
            return Unauthorized(new { error = "User identity could not be determined." });

        var command = new InitierVersementCommand(
            request.TontineId,
            request.TourId,
            payeurId,
            request.NumeroTelephone,
            request.Montant,
            request.Devise ?? "XOF");

        var versementId = await _mediator.Send(command, cancellationToken);
        return Ok(new { versementId });
    }

    // ── POST /api/v1/versements/confirmer (callback Mobile Money) ──
    /// <summary>
    /// Confirm a versement after a successful mobile money callback.
    /// </summary>
    [HttpPost("confirmer")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirmer(
        [FromBody] ConfirmerVersementRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmVersementCommand(request.VersementId, request.ReferenceExterne);
        await _mediator.Send(command, cancellationToken);
        return Ok(new { message = "Versement confirmé." });
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

public sealed record InitierVersementRequest(
    Guid TontineId,
    Guid TourId,
    string NumeroTelephone,
    decimal Montant,
    string? Devise);

public sealed record ConfirmerVersementRequest(
    Guid VersementId,
    string ReferenceExterne);
