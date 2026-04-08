namespace Infrastructure.Sms;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Domain.NotificationManagement.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Africa's Talking SMS adapter with retry x3 (backoff 5min, 15min, 1h).
/// Validates E.164 phone format before sending.
/// </summary>
internal sealed partial class AfricasTalkingSmsAdapter : ISmsGateway
{
    private static readonly Regex E164Regex = GenerateE164Regex();

    private readonly HttpClient _httpClient;
    private readonly ILogger<AfricasTalkingSmsAdapter> _logger;
    private readonly AfricasTalkingSmsOptions _options;

    public AfricasTalkingSmsAdapter(
        HttpClient httpClient,
        ILogger<AfricasTalkingSmsAdapter> logger,
        IOptions<AfricasTalkingSmsOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<SmsResult> EnvoyerAsync(
        string destinataire,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Validate E.164 format before sending
        if (!E164Regex.IsMatch(destinataire))
        {
            _logger.LogWarning(
                "Invalid phone number format: {Destinataire}. Must be E.164.",
                destinataire);
            return new SmsResult(false, null, "Numéro de téléphone invalide (format E.164 requis).");
        }

        var backoffs = _options.RetryBackoffMinutes.Length > 0
            ? _options.RetryBackoffMinutes
            : [5, 15, 60];

        var maxRetries = backoffs.Length;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await SendSmsAsync(destinataire, message, cancellationToken);
                if (result.Success)
                    return result;

                // Non-retryable failure (e.g., invalid response from API)
                if (attempt >= maxRetries)
                    return result;

                var delay = TimeSpan.FromMinutes(backoffs[attempt]);
                _logger.LogWarning(
                    "SMS send attempt {Attempt} failed for {Destinataire}: {Description}. Retrying in {Delay} minutes.",
                    attempt + 1, destinataire, result.Description, delay.TotalMinutes);

                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex,
                        "SMS send failed after {MaxRetries} retries for {Destinataire}",
                        maxRetries + 1, destinataire);
                    return new SmsResult(false, null, $"Échec réseau après {maxRetries + 1} tentatives: {ex.Message}");
                }

                var delay = TimeSpan.FromMinutes(backoffs[attempt]);
                _logger.LogWarning(ex,
                    "Network error on SMS send attempt {Attempt} for {Destinataire}. Retrying in {Delay} minutes.",
                    attempt + 1, destinataire, delay.TotalMinutes);

                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex,
                        "SMS send timed out after {MaxRetries} retries for {Destinataire}",
                        maxRetries + 1, destinataire);
                    return new SmsResult(false, null, $"Timeout après {maxRetries + 1} tentatives.");
                }

                var delay = TimeSpan.FromMinutes(backoffs[attempt]);
                _logger.LogWarning(
                    "Timeout on SMS send attempt {Attempt} for {Destinataire}. Retrying in {Delay} minutes.",
                    attempt + 1, destinataire, delay.TotalMinutes);

                await Task.Delay(delay, cancellationToken);
            }
        }

        return new SmsResult(false, null, "Échec d'envoi après toutes les tentatives.");
    }

    private async Task<SmsResult> SendSmsAsync(
        string destinataire,
        string message,
        CancellationToken cancellationToken)
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = _options.Username,
            ["to"] = destinataire,
            ["message"] = message,
            ["from"] = _options.ShortCode
        });

        _logger.LogInformation(
            "Sending SMS to {Destinataire} via Africa's Talking (length: {Length} chars)",
            destinataire, message.Length);

        var response = await _httpClient.PostAsync("messaging", formData, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Africa's Talking SMS API returned {StatusCode}: {Body}",
                response.StatusCode, responseBody);

            return new SmsResult(false, null, $"HTTP {(int)response.StatusCode}: {responseBody}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<AfricasTalkingSmsResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var recipient = result?.SMSMessageData?.Recipients?.FirstOrDefault();
            var success = recipient != null &&
                          string.Equals(recipient.Status, "Success", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "SMS to {Destinataire}: status={Status}, messageId={MessageId}",
                destinataire, recipient?.Status, recipient?.MessageId);

            return new SmsResult(success, recipient?.MessageId, recipient?.Status);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Africa's Talking SMS response: {Body}", responseBody);
            return new SmsResult(false, null, $"Erreur de parsing: {ex.Message}");
        }
    }

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex GenerateE164Regex();

    private sealed class AfricasTalkingSmsResponse
    {
        public AfricasTalkingSmsMessageData? SMSMessageData { get; set; }
    }

    private sealed class AfricasTalkingSmsMessageData
    {
        public string? Message { get; set; }
        public List<AfricasTalkingSmsRecipient>? Recipients { get; set; }
    }

    private sealed class AfricasTalkingSmsRecipient
    {
        public int StatusCode { get; set; }
        public string? Number { get; set; }
        public string? Status { get; set; }
        public string? Cost { get; set; }
        public string? MessageId { get; set; }
    }
}
