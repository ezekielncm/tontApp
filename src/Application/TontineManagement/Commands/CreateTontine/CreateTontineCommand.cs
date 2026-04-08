namespace Application.TontineManagement.Commands.CreateTontine;

using Application.Common;

public sealed record CreateTontineCommand(
    string Name,
    string Description,
    decimal ContributionAmount,
    string Periodicity,
    int MaxMembers) : ICommand<Guid>;
