namespace Application.PaymentManagement.Commands.CreateVersement;

using Application.Common;
using Domain.Common;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class CreateVersementCommandHandler : ICommandHandler<CreateVersementCommand, Guid>
{
    private readonly IVersementRepository _versementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVersementCommandHandler(IVersementRepository versementRepository, IUnitOfWork unitOfWork)
    {
        _versementRepository = versementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateVersementCommand request, CancellationToken cancellationToken)
    {
        var versement = Versement.Create(
            TontineId.From(request.TontineId),
            MemberId.From(request.MemberId),
            RoundId.From(request.RoundId),
            request.Montant,
            request.Currency);

        await _versementRepository.AddAsync(versement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return versement.Id.Value;
    }
}
