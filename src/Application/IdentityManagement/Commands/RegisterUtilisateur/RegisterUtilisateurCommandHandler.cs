namespace Application.IdentityManagement.Commands.RegisterUtilisateur;

using Application.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;

public sealed class RegisterUtilisateurCommandHandler : ICommandHandler<RegisterUtilisateurCommand, Guid>
{
    private readonly IUtilisateurRepository _utilisateurRepository;

    public RegisterUtilisateurCommandHandler(IUtilisateurRepository utilisateurRepository)
    {
        _utilisateurRepository = utilisateurRepository;
    }

    public async Task<Guid> Handle(RegisterUtilisateurCommand request, CancellationToken cancellationToken)
    {
        var existing = await _utilisateurRepository.GetByTelephoneAsync(request.Telephone, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A user with telephone {request.Telephone} already exists.");

        // In a real implementation, the password would be hashed by an infrastructure service
        var utilisateur = Utilisateur.Create(
            request.Telephone,
            request.Nom,
            request.MotDePasse);

        await _utilisateurRepository.AddAsync(utilisateur, cancellationToken);

        return utilisateur.Id.Value;
    }
}
