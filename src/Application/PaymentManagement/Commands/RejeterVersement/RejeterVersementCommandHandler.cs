namespace Application.PaymentManagement.Commands.RejeterVersement;

using Application.Common;
using Domain.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;

public sealed class RejeterVersementCommandHandler : ICommandHandler<RejeterVersementCommand>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejeterVersementCommandHandler(IVersementRepository versementRepository, IUnitOfWork unitOfWork)
    {
        _versementRepository = versementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejeterVersementCommand request, CancellationToken cancellationToken)
    {
        var versement = await _versementRepository.GetByIdAsync(
            VersementId.From(request.VersementId), cancellationToken)
            ?? throw new InvalidOperationException($"Versement {request.VersementId} not found.");

        versement.Rejeter(request.Raison);

        await _versementRepository.UpdateAsync(versement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
