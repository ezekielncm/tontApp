namespace Api.Middleware;

using System.IdentityModel.Tokens.Jwt;
using Application.IdentityManagement.Services;

public sealed class AccessTokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public AccessTokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAccessTokenBlacklistService blacklistService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti) && await blacklistService.IsBlacklistedAsync(jti))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token has been revoked." });
                return;
            }
        }

        await _next(context);
    }
}
