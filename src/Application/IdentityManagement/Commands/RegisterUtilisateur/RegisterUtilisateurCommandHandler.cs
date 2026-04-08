namespace Application.IdentityManagement.Commands.RegisterUtilisateur;

using Application.Common;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;

public sealed class RegisterUtilisateurCommandHandler : ICommandHandler<RegisterUtilisateurCommand, Guid>
{
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUtilisateurCommandHandler(
        IUtilisateurRepository utilisateurRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _utilisateurRepository = utilisateurRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUtilisateurCommand request, CancellationToken cancellationToken)
    {
        var existing = await _utilisateurRepository.GetByTelephoneAsync(request.Telephone, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A user with telephone {request.Telephone} already exists.");

        var hash = _passwordHasher.Hash(request.MotDePasse);

        var utilisateur = Utilisateur.Create(
            request.Telephone,
            request.Nom,
            hash);

        await _utilisateurRepository.AddAsync(utilisateur, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return utilisateur.Id.Value;
    }
}
