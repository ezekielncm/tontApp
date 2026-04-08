namespace Application.TontineManagement.Commands.GenererCodeInvitation;

using Application.Common;

/// <summary>
/// Generates an invitation code for a tontine. The code is stored hashed in the database.
/// Returns the plain-text code and a deep link for the mobile app.
/// </summary>
public sealed record GenererCodeInvitationCommand(
    Guid TontineId,
    int NombreUsagesMax = 1,
    int ExpirationJours = 7) : ICommand<GenererCodeInvitationResult>;

/// <summary>
/// Result containing the plain-text code, deep link, and expiration.
/// </summary>
public sealed record GenererCodeInvitationResult(
    string Code,
    string DeepLink,
    DateTime Expiration);
