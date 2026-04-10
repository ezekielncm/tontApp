namespace Application.IdentityManagement.Commands.ChangerRole;

using Application.Common;
using Domain.Common;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;

public sealed class ChangerRoleCommandHandler : ICommandHandler<ChangerRoleCommand>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangerRoleCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangerRoleCommand request, CancellationToken cancellationToken)
    {
        var utilisateur = await _utilisateurRepository.GetByIdAsync(
            UtilisateurId.From(request.UtilisateurId), cancellationToken)
            ?? throw new InvalidOperationException($"Utilisateur {request.UtilisateurId} not found.");

        if (!Enum.TryParse<RoleUtilisateur>(request.NouveauRole, ignoreCase: true, out var role))
            throw new InvalidOperationException($"Rôle invalide : {request.NouveauRole}. Valeurs valides : Membre, Gestionnaire, Admin.");

        utilisateur.ChangerRole(role);
        await _utilisateurRepository.UpdateAsync(utilisateur, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
