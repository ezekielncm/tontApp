namespace Domain.IdentityManagement.Repositories;

using Domain.IdentityManagement.ValueObjects;

public interface IUtilisateurRepository
{
    Task<Utilisateur?> GetByIdAsync(UtilisateurId id, CancellationToken cancellationToken = default);
    Task<Utilisateur?> GetByTelephoneAsync(string telephone, CancellationToken cancellationToken = default);
    Task AddAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default);
    Task UpdateAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default);
}
