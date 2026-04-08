namespace Application.TontineManagement.Commands.CloturerTour;

using Application.Common;

public sealed record CloturerTourCommand(Guid TontineId, Guid RoundId) : ICommand;
