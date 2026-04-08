namespace Application.BillingManagement.Commands.CreateAbonnement;

using Application.Common;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;

public sealed class CreateAbonnementCommandHandler : ICommandHandler<CreateAbonnementCommand, Guid>
{
    private readonly IAbonnementRepository _abonnementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAbonnementCommandHandler(IAbonnementRepository abonnementRepository, IUnitOfWork unitOfWork)
    {
        _abonnementRepository = abonnementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAbonnementCommand request, CancellationToken cancellationToken)
    {
        var plan = Enum.Parse<PlanTarifaire>(request.Plan, ignoreCase: true);

        var abonnement = Abonnement.Create(request.GestionnaireId, plan);

        await _abonnementRepository.AddAsync(abonnement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return abonnement.Id.Value;
    }
}
