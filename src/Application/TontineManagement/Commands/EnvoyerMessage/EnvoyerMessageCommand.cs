namespace Application.TontineManagement.Commands.EnvoyerMessage;

using Application.Common;

public sealed record EnvoyerMessageCommand(
    Guid TontineId,
    Guid GestionnaireId,
    string Message) : ICommand<Guid>;
