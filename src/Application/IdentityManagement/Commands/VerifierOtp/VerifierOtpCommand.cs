namespace Application.IdentityManagement.Commands.VerifierOtp;

using Application.Common;
using Application.IdentityManagement.DTOs;

public sealed record VerifierOtpCommand(string Telephone, string Code) : ICommand<AuthResult>;
