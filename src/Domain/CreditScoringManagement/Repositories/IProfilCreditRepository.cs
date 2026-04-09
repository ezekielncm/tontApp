namespace Domain.CreditScoringManagement.Repositories;

using Domain.CreditScoringManagement.ValueObjects;

public interface IProfilCreditRepository
{
    Task<ProfilCredit?> GetByIdAsync(ProfilCreditId id, CancellationToken cancellationToken = default);
    Task<ProfilCredit?> GetByMembreIdAsync(Guid membreId, CancellationToken cancellationToken = default);
    Task AddAsync(ProfilCredit profilCredit, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProfilCredit profilCredit, CancellationToken cancellationToken = default);
}
