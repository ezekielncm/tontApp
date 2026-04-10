namespace Application.IdentityManagement.Commands.ChangerRole;

using Application.Common;

public sealed record ChangerRoleCommand(
    Guid UtilisateurId,
    string NouveauRole) : ICommand;
