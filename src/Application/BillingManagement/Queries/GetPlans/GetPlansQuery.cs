namespace Application.BillingManagement.Queries.GetPlans;

using Application.Common;

public sealed record GetPlansQuery() : IQuery<IReadOnlyList<PlanDto>>;

public sealed record PlanDto(
    Guid Id,
    string Nom,
    string Code,
    decimal PrixMensuel,
    string Devise,
    int MaxTontines,
    int MaxMembresParTontine,
    string? Description);
