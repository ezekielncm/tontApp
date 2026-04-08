namespace Application.TontineManagement.Queries.GetTontineById;

using Application.Common;

public sealed record GetTontineByIdQuery(Guid TontineId) : IQuery<TontineDto?>;
