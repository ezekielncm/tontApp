namespace Application.PaymentManagement.Commands.ConfirmVersement;

using Application.Common;
using Domain.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;

public sealed class ConfirmVersementCommandHandler : ICommandHandler<ConfirmVersementCommand>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmVersementCommandHandler(IVersementRepository versementRepository, IUnitOfWork unitOfWork)
    {
        _versementRepository = versementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ConfirmVersementCommand request, CancellationToken cancellationToken)
    {
        var versement = await _versementRepository.GetByIdAsync(
            VersementId.From(request.VersementId), cancellationToken)
            ?? throw new InvalidOperationException($"Versement {request.VersementId} not found.");

        versement.Confirmer(request.ReferenceExterne);

        await _versementRepository.UpdateAsync(versement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
