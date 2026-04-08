namespace Application.IdentityManagement.Queries.GetUtilisateurByTelephone;

using Application.Common;
using Domain.IdentityManagement.Repositories;

public sealed class GetUtilisateurByTelephoneQueryHandler
    : IQueryHandler<GetUtilisateurByTelephoneQuery, UtilisateurDto?>
{
    private readonly IUtilisateurRepository _utilisateurRepository;

    public GetUtilisateurByTelephoneQueryHandler(IUtilisateurRepository utilisateurRepository)
    {
        _utilisateurRepository = utilisateurRepository;
    }

    public async Task<UtilisateurDto?> Handle(
        GetUtilisateurByTelephoneQuery request,
        CancellationToken cancellationToken)
    {
        var utilisateur = await _utilisateurRepository.GetByTelephoneAsync(
            request.Telephone, cancellationToken);

        if (utilisateur is null)
            return null;

        return new UtilisateurDto(
            utilisateur.Id.Value,
            utilisateur.Telephone.Value,
            utilisateur.Nom,
            utilisateur.Role.ToString(),
            utilisateur.EstActif,
            utilisateur.CreatedAt);
    }
}
