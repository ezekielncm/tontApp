namespace Application.IdentityManagement.Commands.Deconnecter;

using Application.Common;

public sealed record DeconnecterCommand(Guid UtilisateurId, string? Jti = null) : ICommand;
