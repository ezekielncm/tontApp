namespace Application.IdentityManagement.Services;

using Domain.IdentityManagement;

public interface IJwtService
{
    string GenerateAccessToken(Utilisateur utilisateur);
}
