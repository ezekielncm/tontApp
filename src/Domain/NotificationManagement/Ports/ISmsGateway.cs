namespace Domain.NotificationManagement.Ports;

/// <summary>
/// Port for SMS sending gateway (Africa's Talking SMS API).
/// </summary>
public interface ISmsGateway
{
    /// <summary>
    /// Sends an SMS to the specified phone number.
    /// </summary>
    /// <param name="destinataire">Phone number in E.164 format.</param>
    /// <param name="message">Message text (max 160 chars).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the SMS send attempt.</returns>
    Task<SmsResult> EnvoyerAsync(string destinataire, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an SMS send attempt.
/// </summary>
public sealed record SmsResult(
    bool Success,
    string? MessageId,
    string? Description);
