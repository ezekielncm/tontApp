namespace Api.Controllers;

using Application.IdentityManagement.Commands.ChangerRole;
using Application.NotificationManagement.Queries.GetNotificationStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Administration endpoints (Admin only).
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Change the role of a user (promote/demote).
    /// </summary>
    /// <param name="utilisateurId">The user ID.</param>
    /// <param name="request">New role details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Role changed successfully.</response>
    /// <response code="400">Invalid role or business error.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("utilisateurs/{utilisateurId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangerRole(
        Guid utilisateurId,
        [FromBody] ChangerRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ChangerRoleCommand(utilisateurId, request.NouveauRole);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── GET /api/v1/admin/notifications/stats ──────────────────────
    /// <summary>
    /// Get notification statistics (total sent, failed, pending, by type).
    /// </summary>
    [HttpGet("notifications/stats")]
    [ProducesResponseType(typeof(NotificationStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationStats(CancellationToken cancellationToken)
    {
        var query = new GetNotificationStatsQuery();
        var stats = await _mediator.Send(query, cancellationToken);
        return Ok(stats);
    }
}

/// <summary>
/// Request body for changing a user's role.
/// </summary>
public sealed record ChangerRoleRequest(string NouveauRole);
