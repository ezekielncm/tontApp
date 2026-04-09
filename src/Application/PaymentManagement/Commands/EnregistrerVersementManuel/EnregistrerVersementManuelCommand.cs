namespace Application.PaymentManagement.Commands.EnregistrerVersementManuel;

using Application.Common;

public sealed record EnregistrerVersementManuelCommand(
    Guid TontineId,
    Guid TourId,
    Guid MembreId,
    decimal Montant,
    string DescriptionPreuve,
    string Devise = "XOF") : ICommand<Result>;
