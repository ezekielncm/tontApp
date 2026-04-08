namespace Application.TontineManagement.Commands.AddMember;

using Application.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class AddMemberCommandHandler : ICommandHandler<AddMemberCommand>
{
    private readonly ITontineRepository _tontineRepository;

    public AddMemberCommandHandler(ITontineRepository tontineRepository)
    {
        _tontineRepository = tontineRepository;
    }

    public async Task Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        tontine.AddMember(request.MemberName);

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
    }
}
