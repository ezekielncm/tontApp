namespace Application.TontineManagement.Commands.ActivateTontine;

using Application.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class ActivateTontineCommandHandler : ICommandHandler<ActivateTontineCommand>
{
    private readonly ITontineRepository _tontineRepository;

    public ActivateTontineCommandHandler(ITontineRepository tontineRepository)
    {
        _tontineRepository = tontineRepository;
    }

    public async Task Handle(ActivateTontineCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        tontine.Activate();

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
    }
}
