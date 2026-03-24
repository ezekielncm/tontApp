namespace Domain.PaymentManagement.Repositories;

using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public interface IVersementRepository
{
    Task<Versement?> GetByIdAsync(VersementId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Versement>> GetByTontineAndRoundAsync(TontineId tontineId, RoundId roundId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Versement>> GetByMemberAsync(MemberId memberId, CancellationToken cancellationToken = default);
    Task AddAsync(Versement versement, CancellationToken cancellationToken = default);
    Task UpdateAsync(Versement versement, CancellationToken cancellationToken = default);
}
