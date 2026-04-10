namespace Application.TontineManagement.Queries.GetTourActuel;

using Application.Common;

public sealed record GetTourActuelQuery(Guid TontineId) : IQuery<TourActuelDto?>;
