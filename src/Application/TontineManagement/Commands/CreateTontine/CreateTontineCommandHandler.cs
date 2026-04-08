namespace Application.TontineManagement.Commands.CreateTontine;

using Application.Common;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class CreateTontineCommandHandler : ICommandHandler<CreateTontineCommand, Guid>
{
    private readonly ITontineRepository _tontineRepository;

    public CreateTontineCommandHandler(ITontineRepository tontineRepository)
    {
        _tontineRepository = tontineRepository;
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

        return tontine.Id.Value;
    }
}
