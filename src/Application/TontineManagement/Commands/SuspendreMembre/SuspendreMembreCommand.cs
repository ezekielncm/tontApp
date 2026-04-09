namespace Application.TontineManagement.Commands.SuspendreMembre;

using Application.Common;

public sealed record SuspendreMembreCommand(Guid TontineId, Guid MembreId, string Motif) : ICommand<Result>;
