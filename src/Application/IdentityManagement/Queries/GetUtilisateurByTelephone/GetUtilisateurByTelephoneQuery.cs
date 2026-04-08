namespace Application.IdentityManagement.Queries.GetUtilisateurByTelephone;

using Application.Common;

public sealed record GetUtilisateurByTelephoneQuery(string Telephone) : IQuery<UtilisateurDto?>;
