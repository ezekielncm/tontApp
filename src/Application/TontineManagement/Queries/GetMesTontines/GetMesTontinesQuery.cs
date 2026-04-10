namespace Application.TontineManagement.Queries.GetMesTontines;

using Application.Common;
using Application.TontineManagement.Queries.GetTontineById;

public sealed record GetMesTontinesQuery(Guid GestionnaireId) : IQuery<IReadOnlyList<TontineDto>>;
