namespace Application.BillingManagement.Commands.RenewAbonnement;

using Application.Common;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;

public sealed class RenewAbonnementCommandHandler : ICommandHandler<RenewAbonnementCommand>
{
    private readonly IAbonnementRepository _abonnementRepository;

    public RenewAbonnementCommandHandler(IAbonnementRepository abonnementRepository)
    {
        _abonnementRepository = abonnementRepository;
    }

    public async Task Handle(RenewAbonnementCommand request, CancellationToken cancellationToken)
    {
        var abonnement = await _abonnementRepository.GetByIdAsync(
            AbonnementId.From(request.AbonnementId), cancellationToken)
            ?? throw new InvalidOperationException($"Abonnement {request.AbonnementId} not found.");

        abonnement.Renouveler();

        await _abonnementRepository.UpdateAsync(abonnement, cancellationToken);
    }
}
