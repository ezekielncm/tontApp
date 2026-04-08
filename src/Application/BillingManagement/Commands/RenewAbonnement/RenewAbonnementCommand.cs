namespace Application.BillingManagement.Commands.RenewAbonnement;

using Application.Common;

public sealed record RenewAbonnementCommand(Guid AbonnementId) : ICommand;
