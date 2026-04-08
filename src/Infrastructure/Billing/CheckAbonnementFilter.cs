namespace Infrastructure.Billing;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.BillingManagement.Services;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Action filter that verifies subscription plan limits before allowing tontine creation.
/// Uses Redis cache for performant limit checking (no DB query on each request).
/// Falls back to DB only when cache is cold, then populates the cache.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CheckAbonnementFilter : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CheckAbonnementFilter>>();
        var billingCache = context.HttpContext.RequestServices.GetRequiredService<IBillingCacheService>();
        var abonnementRepo = context.HttpContext.RequestServices.GetRequiredService<IAbonnementRepository>();
        var planRepo = context.HttpContext.RequestServices.GetRequiredService<IPlanAbonnementRepository>();

        // Extract user ID from JWT claims
        var userId = context.HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Identité utilisateur non déterminée." });
            return;
        }

        // Get plan limits from Redis cache
        var planLimits = await billingCache.GetPlanLimitsAsync(userId);

        if (planLimits is null)
        {
            // Cache miss: fall back to DB and populate cache
            var abonnement = await abonnementRepo.GetByGestionnaireAsync(userId);

            if (abonnement is null || !abonnement.EstFonctionnellementActif())
            {
                // No active subscription: apply free plan limits
                var gratuitPlan = await planRepo.GetByCodeAsync(PlanAbonnement.Codes.Gratuit);
                if (gratuitPlan is not null)
                {
                    planLimits = new PlanLimitsCache(gratuitPlan.MaxTontines, gratuitPlan.MaxMembresParTontine);
                    await billingCache.SetPlanLimitsAsync(userId, gratuitPlan.MaxTontines, gratuitPlan.MaxMembresParTontine);
                }
                else
                {
                    // Fallback: default free plan limits
                    planLimits = new PlanLimitsCache(1, 10);
                    await billingCache.SetPlanLimitsAsync(userId, 1, 10);
                }
            }
            else
            {
                var plan = await planRepo.GetByIdAsync(abonnement.PlanId);
                if (plan is not null)
                {
                    planLimits = new PlanLimitsCache(plan.MaxTontines, plan.MaxMembresParTontine);
                    await billingCache.SetPlanLimitsAsync(userId, plan.MaxTontines, plan.MaxMembresParTontine);
                }
                else
                {
                    // Fallback to free plan limits
                    planLimits = new PlanLimitsCache(1, 10);
                    await billingCache.SetPlanLimitsAsync(userId, 1, 10);
                }
            }
        }

        // Get current tontine count from Redis
        var currentCount = await billingCache.GetTontineCountAsync(userId);

        if (currentCount is null)
        {
            // Cache miss: initialize from 0 (best effort for MVP)
            currentCount = 0;
            await billingCache.SetTontineCountAsync(userId, 0);
        }

        // Check limit
        if (currentCount >= planLimits.MaxTontines)
        {
            logger.LogWarning(
                "Subscription limit reached for user {UserId}: {Current}/{Max} tontines",
                userId, currentCount, planLimits.MaxTontines);

            context.Result = new ObjectResult(new
            {
                error = $"Limite du plan atteinte : {currentCount}/{planLimits.MaxTontines} tontines. Passez au plan supérieur.",
                currentCount,
                maxTontines = planLimits.MaxTontines
            })
            {
                StatusCode = 403
            };
            return;
        }

        // Execute the action
        var result = await next();

        // If the action succeeded and returned a 201 Created (tontine was actually created),
        // increment the counter
        if (result.Exception is null && context.HttpContext.Response.StatusCode == 201)
        {
            await billingCache.IncrementTontineCountAsync(userId);
        }
    }
}
