namespace Application.TontineManagement.Commands.ActivateTontine;

using Application.Common;

public sealed record ActivateTontineCommand(Guid TontineId) : ICommand;
