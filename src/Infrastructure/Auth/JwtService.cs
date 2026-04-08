namespace Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

internal sealed class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Utilisateur utilisateur)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var issuer = jwtSection["Issuer"] ?? "TontinesApp";
        var audience = jwtSection["Audience"] ?? "TontinesApp";
        var expirationMinutes = int.Parse(jwtSection["AccessTokenExpirationInMinutes"] ?? "1440"); // 24h default

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, utilisateur.Id.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("telephone", utilisateur.Telephone.Value),
            new Claim("nom", utilisateur.Nom),
            new Claim(ClaimTypes.Role, utilisateur.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
