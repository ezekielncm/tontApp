namespace Application.TontineManagement.Commands.CreateTontine;

using Application.Common;
using Domain.Common;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class CreateTontineCommandHandler : ICommandHandler<CreateTontineCommand, Guid>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTontineCommandHandler(ITontineRepository tontineRepository, IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTontineCommand request, CancellationToken cancellationToken)
    {
        var periodicity = Enum.Parse<TontinePeriodicity>(request.Periodicity, ignoreCase: true);
        var contributionAmount = ContributionAmount.Create(request.ContributionAmount, "XOF");

        var tontine = Tontine.Create(
            request.Name,
            request.Description,
            contributionAmount,
            periodicity,
            request.MaxMembers);

        await _tontineRepository.AddAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tontine.Id.Value;
    }
}
