namespace Application.PaymentManagement.Commands.CreateVersement;

using Application.Common;

public sealed record CreateVersementCommand(
    Guid TontineId,
    Guid MemberId,
    Guid RoundId,
    decimal Montant,
    string Currency) : ICommand<Guid>;
