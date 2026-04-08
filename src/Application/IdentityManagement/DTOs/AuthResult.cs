namespace Application.IdentityManagement.DTOs;

public sealed record AuthResult(
    Guid UtilisateurId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
