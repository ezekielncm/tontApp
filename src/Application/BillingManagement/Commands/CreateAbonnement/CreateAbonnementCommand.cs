namespace Application.BillingManagement.Commands.CreateAbonnement;

using Application.Common;

public sealed record CreateAbonnementCommand(
    string GestionnaireId,
    string Plan) : ICommand<Guid>;
