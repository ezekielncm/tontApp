namespace Application.TontineManagement.Commands.CloturerTour;

using Application.Common;
using Domain.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class CloturerTourCommandHandler : ICommandHandler<CloturerTourCommand>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloturerTourCommandHandler(ITontineRepository tontineRepository, IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CloturerTourCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        tontine.CloseRound(RoundId.From(request.RoundId));

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
