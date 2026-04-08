namespace Domain.PaymentManagement.Repositories;

using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public interface IVersementRepository
{
    Task<Versement?> GetByIdAsync(VersementId id, CancellationToken cancellationToken = default);
    Task<Versement?> GetByReferenceExterneAsync(string referenceExterne, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Versement>> GetByTontineAndTourAsync(TontineId tontineId, TourId tourId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Versement>> GetByTontineAsync(TontineId tontineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Versement>> GetByPayeurAsync(PayeurId payeurId, CancellationToken cancellationToken = default);
    Task<Versement?> GetLastByTontineAsync(TontineId tontineId, CancellationToken cancellationToken = default);
    Task AddAsync(Versement versement, CancellationToken cancellationToken = default);
    Task UpdateAsync(Versement versement, CancellationToken cancellationToken = default);
}
