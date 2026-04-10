namespace Application.TontineManagement.Commands.CreateTontine;

using Application.Common;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;

public sealed class CreateTontineCommandHandler : ICommandHandler<CreateTontineCommand, Guid>
{
    private readonly ITontineRepository _tontineRepository;
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTontineCommandHandler(
        ITontineRepository tontineRepository,
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork)
    {
        _tontineRepository = tontineRepository;
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTontineCommand request, CancellationToken cancellationToken)
    {
        var periodicity = Enum.Parse<TontinePeriodicity>(request.Periodicity, ignoreCase: true);
        var contributionAmount = ContributionAmount.Create(request.ContributionAmount, "XOF");
        var gestionnaireId = UtilisateurId.From(request.GestionnaireId);

        var tontine = Tontine.Create(
            request.Name,
            request.Description,
            contributionAmount,
            periodicity,
            request.MaxMembers,
            gestionnaireId);

        await _tontineRepository.AddAsync(tontine, cancellationToken);

        // Promote user to Gestionnaire if they are currently a Membre
        var utilisateur = await _utilisateurRepository.GetByIdAsync(gestionnaireId, cancellationToken);
        if (utilisateur is not null && utilisateur.Role == RoleUtilisateur.Membre)
        {
            utilisateur.ChangerRole(RoleUtilisateur.Gestionnaire);
            await _utilisateurRepository.UpdateAsync(utilisateur, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tontine.Id.Value;
    }
}
