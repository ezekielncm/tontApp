namespace Application.TontineManagement.Commands.SuspendreMembre;

using Application.Common;
using Domain.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class SuspendreMembreCommandHandler : ICommandHandler<SuspendreMembreCommand, Result>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SuspendreMembreCommandHandler(ITontineRepository tontineRepository, IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SuspendreMembreCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken);

        if (tontine is null)
            return Result.Failure($"Tontine {request.TontineId} not found.");

        var (isSuccess, error) = tontine.SuspendreMembre(
            MemberId.From(request.MembreId), request.Motif);

        if (!isSuccess)
            return Result.Failure(error!);

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
