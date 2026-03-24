namespace Domain.BillingManagement.Repositories;

using Domain.BillingManagement.ValueObjects;

public interface IAbonnementRepository
{
    Task<Abonnement?> GetByIdAsync(AbonnementId id, CancellationToken cancellationToken = default);
    Task<Abonnement?> GetByGestionnaireAsync(string gestionnaireId, CancellationToken cancellationToken = default);
    Task AddAsync(Abonnement abonnement, CancellationToken cancellationToken = default);
    Task UpdateAsync(Abonnement abonnement, CancellationToken cancellationToken = default);
}
