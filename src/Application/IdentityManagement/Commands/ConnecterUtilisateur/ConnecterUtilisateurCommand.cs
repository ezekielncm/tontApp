namespace Application.IdentityManagement.Commands.ConnecterUtilisateur;

using Application.Common;
using Application.IdentityManagement.DTOs;

public sealed record ConnecterUtilisateurCommand(
    string Telephone,
    string MotDePasse) : ICommand<AuthResult>;
