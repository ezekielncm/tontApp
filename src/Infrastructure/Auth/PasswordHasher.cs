namespace Infrastructure.Auth;

using Application.IdentityManagement.Services;
using BCrypt.Net;

internal sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.HashPassword(password, workFactor: 11);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Verify(password, hash);
    }
}
