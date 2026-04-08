namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using Application.Common;
using Application.IdentityManagement.DTOs;

public sealed record InscrireUtilisateurCommand(
    string Telephone,
    string Nom,
    string MotDePasse) : ICommand<AuthResult>;
