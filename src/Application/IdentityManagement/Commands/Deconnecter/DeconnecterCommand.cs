namespace Application.IdentityManagement.Commands.Deconnecter;

using Application.Common;

public sealed record DeconnecterCommand(Guid UtilisateurId) : ICommand;
