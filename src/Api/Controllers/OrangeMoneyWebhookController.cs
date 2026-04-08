namespace Api.Controllers;

using System.Security.Cryptography;
using System.Text;
using Application.PaymentManagement.Commands.ConfirmVersement;
using Application.PaymentManagement.Commands.RejeterVersement;
using Infrastructure.Payment;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// Webhook endpoint for Orange Money payment notifications via Africa's Talking.
/// HMAC-SHA256 signature is verified BEFORE any processing.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public class OrangeMoneyWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AfricasTalkingOptions _options;
    private readonly ILogger<OrangeMoneyWebhookController> _logger;

    public OrangeMoneyWebhookController(
        IMediator mediator,
        IOptions<AfricasTalkingOptions> options,
        ILogger<OrangeMoneyWebhookController> logger)
    {
        _mediator = mediator;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Receives Orange Money payment webhook notifications from Africa's Talking.
    /// Validates HMAC-SHA256 signature before processing.
    /// Idempotent: processing the same webhook twice will not create duplicate versements.
    /// </summary>
    /// <response code="200">Webhook processed successfully (or already processed - idempotent).</response>
    /// <response code="401">Invalid HMAC signature.</response>
    /// <response code="400">Invalid payload.</response>
    [HttpPost("orange-money")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        // 1. Read raw body for HMAC verification
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        // 2. Validate HMAC-SHA256 signature BEFORE any processing
        var signatureHeader = Request.Headers["X-AfricasTalking-Signature"].FirstOrDefault();
        if (!ValidateHmacSignature(rawBody, signatureHeader))
        {
            _logger.LogWarning("Invalid HMAC signature on Orange Money webhook");
            return Unauthorized(new { error = "Invalid webhook signature." });
        }

        // 3. Parse the webhook payload
        OrangeMoneyWebhookPayload? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<OrangeMoneyWebhookPayload>(
                rawBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { error = "Invalid JSON payload." });
        }

        if (payload is null || string.IsNullOrEmpty(payload.TransactionId))
        {
            return BadRequest(new { error = "Missing required fields in webhook payload." });
        }

        _logger.LogInformation(
            "Processing Orange Money webhook: TransactionId={TransactionId}, Status={Status}",
            payload.TransactionId, payload.Status);

        // 4. Parse the versement ID from the reference/metadata
        if (!Guid.TryParse(payload.RequestMetadata?.Reference, out var versementId))
        {
            _logger.LogWarning(
                "Could not parse VersementId from webhook metadata reference: {Reference}",
                payload.RequestMetadata?.Reference);
            return BadRequest(new { error = "Invalid reference in webhook metadata." });
        }

        // 5. Process based on status
        try
        {
            if (string.Equals(payload.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                var command = new ConfirmVersementCommand(versementId, payload.TransactionId);
                await _mediator.Send(command, cancellationToken);
            }
            else
            {
                var raison = payload.Description ?? $"Payment {payload.Status}";
                var command = new RejeterVersementCommand(versementId, raison);
                await _mediator.Send(command, cancellationToken);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Versement {VersementId} not found for webhook", versementId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException)
        {
            // Idempotence: if versement is already confirmed/rejected, silently succeed
            _logger.LogInformation(
                "Webhook for VersementId={VersementId} already processed (idempotent)",
                versementId);
        }

        return Ok(new { status = "processed" });
    }

    private bool ValidateHmacSignature(string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(_options.WebhookHmacSecret))
        {
            _logger.LogError("WebhookHmacSecret is not configured");
            return false;
        }

        if (string.IsNullOrEmpty(signatureHeader))
            return false;

        var secretBytes = Encoding.UTF8.GetBytes(_options.WebhookHmacSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(secretBytes);
        var computedHash = hmac.ComputeHash(bodyBytes);
        var computedSignature = Convert.ToHexStringLower(computedHash);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signatureHeader));
    }
}

/// <summary>
/// Webhook payload from Africa's Talking Orange Money notification.
/// </summary>
public sealed record OrangeMoneyWebhookPayload
{
    public string? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Provider { get; init; }
    public string? ProviderChannel { get; init; }
    public decimal? Value { get; init; }
    public WebhookMetadata? RequestMetadata { get; init; }
}

public sealed record WebhookMetadata
{
    public string? Reference { get; init; }
}
