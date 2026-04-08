namespace Application.IdentityManagement.Commands.RegisterUtilisateur;

using Application.Common;

public sealed record RegisterUtilisateurCommand(
    string Telephone,
    string Nom,
    string MotDePasse) : ICommand<Guid>;
