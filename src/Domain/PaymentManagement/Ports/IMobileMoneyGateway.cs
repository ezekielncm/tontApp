namespace Domain.PaymentManagement.Ports;

using Domain.PaymentManagement.ValueObjects;

/// <summary>
/// Port for mobile money payment gateway (Orange Money via Africa's Talking).
/// </summary>
public interface IMobileMoneyGateway
{
    /// <summary>
    /// Initiates a mobile money payment.
    /// </summary>
    /// <param name="request">Payment initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment initiation response with external reference.</returns>
    Task<MobileMoneyResponse> InitierPaiementAsync(MobileMoneyRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to initiate a mobile money payment.
/// </summary>
public sealed record MobileMoneyRequest(
    string NumeroTelephone,
    decimal Montant,
    string Devise,
    string Reference);

/// <summary>
/// Response from mobile money payment initiation.
/// </summary>
public sealed record MobileMoneyResponse(
    bool Success,
    string? TransactionId,
    string? Description);
