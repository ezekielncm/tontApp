namespace Application.BillingManagement.Commands.RenewAbonnement;

using Application.Common;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class RenewAbonnementCommandHandler : ICommandHandler<RenewAbonnementCommand>
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RenewAbonnementCommandHandler(IAbonnementRepository abonnementRepository, IUnitOfWork unitOfWork)
    {
        _abonnementRepository = abonnementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RenewAbonnementCommand request, CancellationToken cancellationToken)
    {
        var abonnement = await _abonnementRepository.GetByIdAsync(
            AbonnementId.From(request.AbonnementId), cancellationToken)
            ?? throw new InvalidOperationException($"Abonnement {request.AbonnementId} not found.");

        abonnement.Renouveler();

        await _abonnementRepository.UpdateAsync(abonnement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
