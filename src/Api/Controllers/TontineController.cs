namespace Api.Controllers;

using Application.TontineManagement.Commands.ActivateTontine;
using Application.TontineManagement.Commands.AddMember;
using Application.TontineManagement.Commands.CloturerTour;
using Application.TontineManagement.Commands.CreateTontine;
using Application.TontineManagement.Commands.EnvoyerMessage;
using Application.TontineManagement.Commands.GenererCodeInvitation;
using Application.TontineManagement.Services;
using Application.TontineManagement.Commands.OuvrirTour;
using Application.TontineManagement.Commands.RejoindreParCode;
using Application.TontineManagement.Commands.SuspendreMembre;
using Application.TontineManagement.Queries.GetMesTontines;
using Application.TontineManagement.Queries.GetTontineById;
using Application.TontineManagement.Queries.GetTourActuel;
using Application.PaymentManagement.Queries.GetVersementsByRound;
using Infrastructure.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// Endpoints for managing tontines: creation, members, activation, rounds.
/// </summary>
[ApiController]
[Route("api/v1/tontines")]
[Produces("application/json")]
[Authorize]
public class TontineController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITontineExportService _exportService;

    public TontineController(IMediator mediator, ITontineExportService exportService)
    {
        _mediator = mediator;
        _exportService = exportService;
    }

    /// <summary>
    /// Create a new tontine (Draft status).
    /// </summary>
    /// <param name="request">Tontine creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created tontine.</returns>
    /// <response code="201">Tontine created successfully.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [CheckAbonnementFilter]
    [ProducesResponseType(typeof(CreateTontineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTontineRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var gestionnaireId))
            return Unauthorized(new { error = "User identity could not be determined." });

        var command = new CreateTontineCommand(
            request.Name,
            request.Description,
            request.ContributionAmount,
            request.Periodicity,
            request.MaxMembers,
            gestionnaireId);

        var tontineId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = tontineId }, new CreateTontineResponse(tontineId));
    }

    /// <summary>
    /// Get a tontine by its ID.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tontine details.</returns>
    /// <response code="200">Tontine found.</response>
    /// <response code="404">Tontine not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TontineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTontineByIdQuery(id);
        var tontine = await _mediator.Send(query, cancellationToken);

        if (tontine is null)
            return NotFound(new { error = $"Tontine {id} not found." });

        return Ok(tontine);
    }

    /// <summary>
    /// Add a member to a tontine (only when in Draft status).
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="request">Member details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Member added successfully.</response>
    /// <response code="400">Validation error or business rule violation.</response>
    /// <response code="404">Tontine not found.</response>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(
        Guid id,
        [FromBody] AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AddMemberCommand(id, request.MemberName);
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

    /// <summary>
    /// Activate a tontine (requires minimum 3 members).
    /// Transitions from Draft to Active and opens the first round.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Tontine activated successfully.</response>
    /// <response code="400">Business rule violation (not enough members, wrong status).</response>
    /// <response code="404">Tontine not found.</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Gestionnaire,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ActivateTontineCommand(id);
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

    /// <summary>
    /// Open a new round for an active tontine.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly opened round.</returns>
    /// <response code="201">Round opened successfully.</response>
    /// <response code="400">Business rule violation (round already open, no remaining members).</response>
    /// <response code="404">Tontine not found.</response>
    [HttpPost("{id:guid}/rounds/open")]
    [Authorize(Roles = "Gestionnaire,Admin")]
    [ProducesResponseType(typeof(OpenRoundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OuvrirTour(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new OuvrirTourCommand(id);
            var roundId = await _mediator.Send(command, cancellationToken);
            return Created(string.Empty, new OpenRoundResponse(roundId));
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

    /// <summary>
    /// Close (complete) a round in an active tontine.
    /// Automatically opens the next round if there are remaining beneficiaries.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="roundId">The round ID to close.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Round closed successfully.</response>
    /// <response code="400">Business rule violation (round already closed, wrong status).</response>
    /// <response code="404">Tontine or round not found.</response>
    [HttpPost("{id:guid}/rounds/{roundId:guid}/close")]
    [Authorize(Roles = "Gestionnaire,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloturerTour(
        Guid id,
        Guid roundId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CloturerTourCommand(id, roundId);
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

    /// <summary>
    /// Generate an invitation code for a tontine (only when in Draft status).
    /// The code is stored hashed in the database. The plain-text code and a deep link
    /// for the mobile app are returned.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="nombreUsagesMax">Maximum number of times the code can be used (default: 1).</param>
    /// <param name="expirationJours">Number of days before the code expires (default: 7).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plain-text code, deep link, and expiration date.</returns>
    /// <response code="200">Invitation code generated successfully.</response>
    /// <response code="400">Business rule violation (wrong tontine status).</response>
    /// <response code="404">Tontine not found.</response>
    [HttpGet("{id:guid}/invitation/generer")]
    [ProducesResponseType(typeof(GenererCodeInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenererCodeInvitation(
        Guid id,
        [FromQuery] int nombreUsagesMax = 1,
        [FromQuery] int expirationJours = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new GenererCodeInvitationCommand(id, nombreUsagesMax, expirationJours);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new GenererCodeInvitationResponse(result.Code, result.DeepLink, result.Expiration));
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

    /// <summary>
    /// Join a tontine using an invitation code. The tontine must be in Draft status.
    /// A user cannot join the same tontine twice.
    /// </summary>
    /// <param name="request">Invitation code and member details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Successfully joined the tontine.</response>
    /// <response code="400">Invalid code, expired code, or business rule violation.</response>
    [HttpPost("/api/v1/tontines/rejoindre")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejoindreParCode(
        [FromBody] RejoindreParCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract the authenticated user's ID from the JWT "sub" claim.
            // JwtService stores the user ID in JwtRegisteredClaimNames.Sub which may be
            // mapped to ClaimTypes.NameIdentifier by the JWT middleware depending on config.
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var utilisateurId))
                return Unauthorized(new { error = "User identity could not be determined." });

            var command = new RejoindreParCodeCommand(request.Code, request.MemberName, utilisateurId);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Suspend a member from an active tontine with a reason.
    /// </summary>
    /// <param name="id">The tontine ID.</param>
    /// <param name="membreId">The member ID to suspend.</param>
    /// <param name="request">Suspension details (motif).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Member suspended successfully.</response>
    /// <response code="400">Validation error or business rule violation.</response>
    /// <response code="404">Tontine or member not found.</response>
    [HttpPut("{id:guid}/membres/{membreId:guid}/suspendre")]
    [Authorize(Roles = "Gestionnaire,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendreMembre(
        Guid id,
        Guid membreId,
        [FromBody] SuspendreMembreRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SuspendreMembreCommand(id, membreId, request.Motif);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("not found"))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    // ── GET /api/v1/tontines/mes-tontines ──────────────────────────
    /// <summary>
    /// Get all tontines managed by the authenticated user.
    /// </summary>
    [HttpGet("mes-tontines")]
    [ProducesResponseType(typeof(IReadOnlyList<TontineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMesTontines(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var gestionnaireId))
            return Unauthorized(new { error = "User identity could not be determined." });

        var query = new GetMesTontinesQuery(gestionnaireId);
        var tontines = await _mediator.Send(query, cancellationToken);
        return Ok(tontines);
    }

    // ── GET /api/v1/tontines/{id}/tours/{tourId}/paiements ─────────
    /// <summary>
    /// Get all payments for a specific round of a tontine.
    /// </summary>
    [HttpGet("{id:guid}/tours/{tourId:guid}/paiements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaiementsByRound(
        Guid id,
        Guid tourId,
        CancellationToken cancellationToken)
    {
        var query = new GetVersementsByRoundQuery(id, tourId);
        var versements = await _mediator.Send(query, cancellationToken);
        return Ok(versements);
    }

    // ── GET /api/v1/tontines/{id}/tours/actuel ─────────────────────
    /// <summary>
    /// Get the current (open) round details with payment progress.
    /// </summary>
    [HttpGet("{id:guid}/tours/actuel")]
    [ProducesResponseType(typeof(TourActuelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTourActuel(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTourActuelQuery(id);
        var tour = await _mediator.Send(query, cancellationToken);

        if (tour is null)
            return NotFound(new { error = "Aucun tour en cours pour cette tontine." });

        return Ok(tour);
    }

    // ── POST /api/v1/tontines/{id}/messages ────────────────────────
    /// <summary>
    /// Send a custom message to all active members of a tontine (gestionnaire only).
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [Authorize(Roles = "Gestionnaire,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnvoyerMessage(
        Guid id,
        [FromBody] EnvoyerMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var gestionnaireId))
                return Unauthorized(new { error = "User identity could not be determined." });

            var command = new EnvoyerMessageCommand(id, gestionnaireId, request.Message);
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

    /// <summary>
    /// Export tontine history as PDF.
    /// </summary>
    [HttpGet("{id:guid}/export/pdf")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var pdfBytes = await _exportService.GeneratePdfAsync(id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"tontine-{id:N}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request body for creating a tontine.
/// </summary>
public sealed record CreateTontineRequest(
    string Name,
    string Description,
    decimal ContributionAmount,
    string Periodicity,
    int MaxMembers);

/// <summary>
/// Response after creating a tontine.
/// </summary>
public sealed record CreateTontineResponse(Guid Id);

/// <summary>
/// Request body for adding a member.
/// </summary>
public sealed record AddMemberRequest(string MemberName);

/// <summary>
/// Response after opening a round.
/// </summary>
public sealed record OpenRoundResponse(Guid RoundId);

/// <summary>
/// Response after generating an invitation code.
/// </summary>
public sealed record GenererCodeInvitationResponse(string Code, string DeepLink, DateTime Expiration);

/// <summary>
/// Request body for joining a tontine via invitation code.
/// </summary>
public sealed record RejoindreParCodeRequest(string Code, string MemberName);

/// <summary>
/// Request body for suspending a member.
/// </summary>
public sealed record SuspendreMembreRequest(string Motif);

/// <summary>
/// Request body for sending a custom message to tontine members.
/// </summary>
public sealed record EnvoyerMessageRequest(string Message);
