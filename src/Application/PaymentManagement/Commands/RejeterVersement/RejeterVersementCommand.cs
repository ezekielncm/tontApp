namespace Application.PaymentManagement.Commands.RejeterVersement;

using Application.Common;

public sealed record RejeterVersementCommand(
    Guid VersementId,
    string Raison) : ICommand;
