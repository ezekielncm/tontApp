namespace Application.TontineManagement.Commands.GenererCodeInvitation;

using Application.Common;
using Domain.Common;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class GenererCodeInvitationCommandHandler
    : ICommandHandler<GenererCodeInvitationCommand, GenererCodeInvitationResult>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenererCodeInvitationCommandHandler(
        ITontineRepository tontineRepository,
        IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GenererCodeInvitationResult> Handle(
        GenererCodeInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var tontine = await _tontineRepository.GetByIdAsync(
            TontineId.From(request.TontineId), cancellationToken)
            ?? throw new InvalidOperationException($"Tontine {request.TontineId} not found.");

        var (invitation, plainCode) = tontine.GenerateInvitation(
            request.NombreUsagesMax,
            request.ExpirationJours);

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var deepLink = $"tontinesapp://rejoindre/{plainCode}";

        return new GenererCodeInvitationResult(
            plainCode,
            deepLink,
            invitation.ExpiresAt);
    }
}
