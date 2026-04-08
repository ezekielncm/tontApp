namespace Application.BillingManagement.Commands.CreateAbonnement;

using Application.Common;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;

public sealed class CreateAbonnementCommandHandler : ICommandHandler<CreateAbonnementCommand, Guid>
{
    private readonly IAbonnementRepository _abonnementRepository;

    public CreateAbonnementCommandHandler(IAbonnementRepository abonnementRepository)
    {
        _abonnementRepository = abonnementRepository;
    }

    public async Task<Guid> Handle(CreateAbonnementCommand request, CancellationToken cancellationToken)
    {
        var plan = Enum.Parse<PlanTarifaire>(request.Plan, ignoreCase: true);

        var abonnement = Abonnement.Create(request.GestionnaireId, plan);

        await _abonnementRepository.AddAsync(abonnement, cancellationToken);

        return abonnement.Id.Value;
    }
}
