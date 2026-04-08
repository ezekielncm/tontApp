namespace Application.PaymentManagement.Commands.CreateVersement;

using Application.Common;

public sealed record CreateVersementCommand(
    Guid TontineId,
    Guid PayeurId,
    Guid TourId,
    decimal Montant,
    string Devise = "XOF") : ICommand<Guid>;
