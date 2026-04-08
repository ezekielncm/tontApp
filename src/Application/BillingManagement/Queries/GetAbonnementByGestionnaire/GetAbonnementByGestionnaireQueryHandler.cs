namespace Application.BillingManagement.Queries.GetAbonnementByGestionnaire;

using Application.Common;
using Domain.BillingManagement.Repositories;

public sealed class GetAbonnementByGestionnaireQueryHandler
    : IQueryHandler<GetAbonnementByGestionnaireQuery, AbonnementDto?>
{
    private readonly IAbonnementRepository _abonnementRepository;

    public GetAbonnementByGestionnaireQueryHandler(IAbonnementRepository abonnementRepository)
    {
        _abonnementRepository = abonnementRepository;
    }

    public async Task<AbonnementDto?> Handle(
        GetAbonnementByGestionnaireQuery request,
        CancellationToken cancellationToken)
    {
        var abonnement = await _abonnementRepository.GetByGestionnaireAsync(
            request.GestionnaireId, cancellationToken);

        if (abonnement is null)
            return null;

        return new AbonnementDto(
            abonnement.Id.Value,
            abonnement.GestionnaireId,
            abonnement.Plan.ToString(),
            abonnement.Statut.ToString(),
            abonnement.MontantMensuel,
            abonnement.Currency,
            abonnement.DateDebut,
            abonnement.DateFin,
            abonnement.CreatedAt);
    }
}
