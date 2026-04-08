namespace Application.TontineManagement.Commands.OuvrirTour;

using Application.Common;
using Domain.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class OuvrirTourCommandHandler : ICommandHandler<OuvrirTourCommand, Guid>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OuvrirTourCommandHandler(ITontineRepository tontineRepository, IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(OuvrirTourCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        var round = tontine.OuvrirTour();

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return round.Id.Value;
    }
}
