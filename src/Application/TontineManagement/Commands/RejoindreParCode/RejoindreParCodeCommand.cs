namespace Application.TontineManagement.Commands.RejoindreParCode;

using Application.Common;

/// <summary>
/// Allows a user to join a tontine using an invitation code.
/// Validates the code, checks that the tontine is in Draft status,
/// and ensures the user is not already a member.
/// </summary>
public sealed record RejoindreParCodeCommand(
    string Code,
    string MemberName,
    Guid UtilisateurId) : ICommand;
