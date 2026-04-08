namespace Infrastructure.Sms;

/// <summary>
/// Configuration options for Africa's Talking SMS API.
/// </summary>
public sealed class AfricasTalkingSmsOptions
{
    public const string SectionName = "Sms:AfricasTalking";

    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.africastalking.com/version1";

    /// <summary>
    /// Retry backoff durations in minutes for failed SMS sends.
    /// Default: 5 min, 15 min, 60 min (3 retries).
    /// </summary>
    public int[] RetryBackoffMinutes { get; set; } = [5, 15, 60];
}
