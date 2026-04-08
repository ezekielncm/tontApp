namespace Application.BillingManagement.Commands.SouscrireAbonnement;

using Application.Common;

public sealed record SouscrireAbonnementCommand(
    string GestionnaireId,
    string PlanCode,
    string NumeroTelephone) : ICommand<SouscrireAbonnementResult>;

public sealed record SouscrireAbonnementResult(
    Guid AbonnementId,
    string Plan,
    string Statut,
    decimal MontantMensuel,
    string Currency,
    DateTime DateDebut,
    DateTime DateFin,
    bool PaiementInitie,
    string? TransactionId);
