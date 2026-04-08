namespace Application.TontineManagement.Commands.RejoindreParCode;

using Application.Common;
using Domain.Common;
using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class RejoindreParCodeCommandHandler : ICommandHandler<RejoindreParCodeCommand>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejoindreParCodeCommandHandler(
        ITontineRepository tontineRepository,
        IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejoindreParCodeCommand request, CancellationToken cancellationToken)
    {
        // Hash the provided plain-text code to find the matching invitation
        var codeHash = InvitationCode.ComputeHash(request.Code);

        var tontine = await _tontineRepository.GetByInvitationCodeHashAsync(codeHash, cancellationToken)
            ?? throw new InvalidOperationException("Invalid invitation code.");

        var utilisateurId = UtilisateurId.From(request.UtilisateurId);

        tontine.JoinWithInvitation(request.MemberName, request.Code, utilisateurId);

        await _tontineRepository.UpdateAsync(tontine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
