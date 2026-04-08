namespace Application.IdentityManagement.Queries.GetUtilisateurByTelephone;

public sealed record UtilisateurDto(
    Guid Id,
    string Telephone,
    string Nom,
    string Role,
    bool EstActif,
    DateTime CreatedAt);
