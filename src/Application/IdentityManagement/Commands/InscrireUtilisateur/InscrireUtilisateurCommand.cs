namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using Application.Common;

public sealed record InscrireUtilisateurCommand(
    string Telephone,
    string Nom,
    string MotDePasse) : ICommand<InscrireUtilisateurResult>;

public sealed record InscrireUtilisateurResult(Guid UtilisateurId, string Message);
