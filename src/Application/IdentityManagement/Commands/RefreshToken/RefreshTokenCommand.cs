namespace Application.IdentityManagement.Commands.RefreshToken;

using Application.Common;
using Application.IdentityManagement.DTOs;

public sealed record RefreshTokenCommand(
    string RefreshToken) : ICommand<AuthResult>;
