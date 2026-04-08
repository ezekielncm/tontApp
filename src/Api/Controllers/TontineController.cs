namespace Api.Controllers;

using Application.TontineManagement.Commands.ActivateTontine;
using Application.TontineManagement.Commands.AddMember;
using Application.TontineManagement.Commands.CloturerTour;
using Application.TontineManagement.Commands.CreateTontine;
using Application.TontineManagement.Commands.OuvrirTour;
using Application.TontineManagement.Queries.GetTontineById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public TontineController(IMediator mediator)
    {
        _mediator = mediator;
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
    [ProducesResponseType(typeof(CreateTontineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTontineRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTontineCommand(
            request.Name,
            request.Description,
            request.ContributionAmount,
            request.Periodicity,
            request.MaxMembers);

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
