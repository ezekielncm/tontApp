namespace Application.TontineManagement.Commands.AddMember;

using Application.Common;

public sealed record AddMemberCommand(Guid TontineId, string MemberName) : ICommand;
