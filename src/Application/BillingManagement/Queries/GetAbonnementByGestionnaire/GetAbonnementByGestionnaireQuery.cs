namespace Application.BillingManagement.Queries.GetAbonnementByGestionnaire;

using Application.Common;

public sealed record GetAbonnementByGestionnaireQuery(
    string GestionnaireId) : IQuery<AbonnementDto?>;
