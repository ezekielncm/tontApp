namespace Infrastructure.Jobs;

using Hangfire.Dashboard;

/// <summary>
/// Authorization filter for Hangfire dashboard: only admin users can access.
/// In development, allows all access for convenience.
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In development, allow unrestricted access
        var environment = httpContext.RequestServices
            .GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

        if (environment?.EnvironmentName == "Development")
            return true;

        // In production, require authenticated admin user
        var user = httpContext.User;
        return user.Identity?.IsAuthenticated == true &&
               user.IsInRole("ADMIN");
    }
}
