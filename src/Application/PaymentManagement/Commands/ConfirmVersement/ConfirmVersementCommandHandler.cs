namespace Application.PaymentManagement.Commands.ConfirmVersement;

using Application.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;

public sealed class ConfirmVersementCommandHandler : ICommandHandler<ConfirmVersementCommand>
{
    private readonly IVersementRepository _versementRepository;

    public ConfirmVersementCommandHandler(IVersementRepository versementRepository)
    {
        _versementRepository = versementRepository;
    }

    public async Task Handle(ConfirmVersementCommand request, CancellationToken cancellationToken)
    {
        var versement = await _versementRepository.GetByIdAsync(
            VersementId.From(request.VersementId), cancellationToken)
            ?? throw new InvalidOperationException($"Versement {request.VersementId} not found.");

        versement.Confirmer(request.ReferenceExterne);

        await _versementRepository.UpdateAsync(versement, cancellationToken);
    }
}
