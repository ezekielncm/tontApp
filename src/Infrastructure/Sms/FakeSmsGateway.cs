namespace Infrastructure.Sms;

using Domain.NotificationManagement.Ports;
using Microsoft.Extensions.Logging;

/// <summary>
/// Fake SMS gateway for development — logs messages to console instead of sending.
/// OTP codes will appear in the application logs.
/// </summary>
public sealed class FakeSmsGateway : ISmsGateway
{
    private readonly ILogger<FakeSmsGateway> _logger;

    public FakeSmsGateway(ILogger<FakeSmsGateway> logger)
    {
        _logger = logger;
    }

    public Task<SmsResult> EnvoyerAsync(string destinataire, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FakeSMS] To: {To} | Message: {Message}", destinataire, message);
        return Task.FromResult(new SmsResult(true, $"fake-{Guid.NewGuid():N}", "Logged to console"));
    }
}
