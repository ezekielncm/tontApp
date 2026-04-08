namespace Api.Controllers;

using System.Security.Claims;
using Application.IdentityManagement.Commands.ConnecterUtilisateur;
using Application.IdentityManagement.Commands.Deconnecter;
using Application.IdentityManagement.Commands.InscrireUtilisateur;
using Application.IdentityManagement.Commands.RefreshToken;
using Application.IdentityManagement.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Authentication endpoints for TontinesApp.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user with a phone number and password.
    /// </summary>
    /// <param name="request">Registration details (phone in E.164 format, name, password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT access and refresh tokens.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Validation error (invalid phone format, weak password, etc.).</response>
    /// <response code="409">A user with this phone number already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new InscrireUtilisateurCommand(
                request.Telephone,
                request.Nom,
                request.MotDePasse);

            var result = await _mediator.Send(command, cancellationToken);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("existe déjà"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticate a user and get JWT tokens.
    /// </summary>
    /// <param name="request">Login credentials (phone in E.164 format, password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT access and refresh tokens.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Invalid credentials or account locked.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ConnecterUtilisateurCommand(
                request.Telephone,
                request.MotDePasse);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh the JWT access token using a refresh token.
    /// The old refresh token is invalidated (rotation).
    /// </summary>
    /// <param name="request">The current refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New JWT access and refresh tokens.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Logout the current user by revoking their refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Logout successful.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var command = new DeconnecterCommand(userId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Registration request body.
/// </summary>
public sealed record RegisterRequest(string Telephone, string Nom, string MotDePasse);

/// <summary>
/// Login request body.
/// </summary>
public sealed record LoginRequest(string Telephone, string MotDePasse);

/// <summary>
/// Refresh token request body.
/// </summary>
public sealed record RefreshRequest(string RefreshToken);
