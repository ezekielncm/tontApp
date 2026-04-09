namespace Api.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Common;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
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

    public MembreController(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
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
}

/// <summary>
/// Request body for updating the FCM push token.
/// </summary>
public sealed record UpdateFcmTokenRequest(
    [Required]
    [StringLength(500, MinimumLength = 1)]
    string Token);
