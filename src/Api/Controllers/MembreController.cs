namespace Api.Controllers;

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.PaymentManagement.Queries.GetMesVersements;
using Domain.Common;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Endpoints for the authenticated member (current user).
/// </summary>
[ApiController]
[Route("api/v1/membres")]
[Produces("application/json")]
[Authorize]
public class MembreController : ControllerBase
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public MembreController(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    /// <summary>
    /// Register or update the FCM push token for the authenticated user.
    /// </summary>
    /// <param name="request">The push token to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Token updated successfully.</response>
    /// <response code="400">Invalid request (missing token).</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("moi/fcm-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFcmToken(
        [FromBody] UpdateFcmTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var utilisateur = await _utilisateurRepository.GetByIdAsync(
            UtilisateurId.From(userId), cancellationToken);

        if (utilisateur is null)
            return NotFound(new { error = "Utilisateur introuvable." });

        utilisateur.MettreAJourFcmToken(request.Token);
        await _utilisateurRepository.UpdateAsync(utilisateur, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ── GET /api/v1/membres/moi/versements ─────────────────────────
    /// <summary>
    /// Get all payments made by the authenticated user.
    /// Optionally filter by tontine ID.
    /// </summary>
    [HttpGet("moi/versements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMesVersements(
        [FromQuery] Guid? tontineId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var query = new GetMesVersementsQuery(userId, tontineId);
        var versements = await _mediator.Send(query, cancellationToken);
        return Ok(versements);
    }

    // ── PUT /api/v1/membres/moi/sms-preferences ────────────────────
    /// <summary>
    /// Update SMS opt-out preference for the authenticated user.
    /// When opted out, only critical SMS (payment confirmations) are sent.
    /// </summary>
    [HttpPut("moi/sms-preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSmsPreferences(
        [FromBody] UpdateSmsPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var utilisateur = await _utilisateurRepository.GetByIdAsync(
            UtilisateurId.From(userId), cancellationToken);

        if (utilisateur is null)
            return NotFound(new { error = "Utilisateur introuvable." });

        if (request.OptOut)
            utilisateur.DesactiverSms();
        else
            utilisateur.ReactiverSms();

        await _utilisateurRepository.UpdateAsync(utilisateur, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

/// <summary>
/// Request body for updating the FCM push token.
/// </summary>
public sealed record UpdateFcmTokenRequest(
    [Required]
    [StringLength(500, MinimumLength = 1)]
    string Token);

/// <summary>
/// Request body for updating SMS opt-out preference.
/// </summary>
public sealed record UpdateSmsPreferencesRequest(bool OptOut);
