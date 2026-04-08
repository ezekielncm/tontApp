namespace Application.TontineManagement.Commands.OuvrirTour;

using Application.Common;

public sealed record OuvrirTourCommand(Guid TontineId) : ICommand<Guid>;
