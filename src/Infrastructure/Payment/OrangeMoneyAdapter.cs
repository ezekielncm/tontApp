namespace Infrastructure.Payment;

using System.Net.Http.Json;
using System.Text.Json;
using Domain.PaymentManagement.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Orange Money payment adapter using Africa's Talking Payment API.
/// Implements the IMobileMoneyGateway port.
/// </summary>
internal sealed class OrangeMoneyAdapter : IMobileMoneyGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrangeMoneyAdapter> _logger;
    private readonly AfricasTalkingOptions _options;

    public OrangeMoneyAdapter(
        HttpClient httpClient,
        ILogger<OrangeMoneyAdapter> logger,
        IOptions<AfricasTalkingOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<MobileMoneyResponse> InitierPaiementAsync(
        MobileMoneyRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initiating Orange Money payment for reference {Reference}, amount {Amount} {Currency}",
            request.Reference, request.Montant, request.Devise);

        var payload = new
        {
            username = _options.Username,
            productName = _options.ProductName,
            phoneNumber = request.NumeroTelephone,
            currencyCode = request.Devise,
            amount = request.Montant,
            metadata = new Dictionary<string, string>
            {
                ["reference"] = request.Reference
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "mobile/checkout/request",
                payload,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Orange Money payment initiation failed for reference {Reference}: {StatusCode} - {Body}",
                    request.Reference, response.StatusCode, responseBody);

                return new MobileMoneyResponse(
                    Success: false,
                    TransactionId: null,
                    Description: $"HTTP {(int)response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<AfricasTalkingCheckoutResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var success = string.Equals(result?.Status, "PendingConfirmation", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Orange Money payment initiation result for reference {Reference}: {Status}",
                request.Reference, result?.Status);

            return new MobileMoneyResponse(
                Success: success,
                TransactionId: result?.TransactionId,
                Description: result?.Description);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Timeout while initiating Orange Money payment for reference {Reference}",
                request.Reference);

            return new MobileMoneyResponse(
                Success: false,
                TransactionId: null,
                Description: "Request timed out after 10 seconds.");
        }
    }

    private sealed record AfricasTalkingCheckoutResponse(
        string? Status,
        string? Description,
        string? TransactionId);
}

/// <summary>
/// Configuration options for Africa's Talking API.
/// </summary>
public sealed class AfricasTalkingOptions
{
    public const string SectionName = "AfricasTalking";

    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://payments.africastalking.com";
    public string ProductName { get; set; } = string.Empty;
    public string WebhookHmacSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
}
