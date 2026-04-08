namespace Infrastructure;

using Domain.BillingManagement.Repositories;
using Domain.Common;
using Domain.IdentityManagement.Repositories;
using Domain.NotificationManagement.Repositories;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core - PostgreSQL
        services.AddDbContext<TontineDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TontineDbContext>());

        // Repositories
        services.AddScoped<ITontineRepository, TontineRepository>();
        services.AddScoped<IVersementRepository, VersementRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUtilisateurRepository, UtilisateurRepository>();
        services.AddScoped<IAbonnementRepository, AbonnementRepository>();

        // Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "TontinesApp:";
        });

        // Hangfire background jobs
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(configuration.GetConnectionString("HangfireConnection"))));

        services.AddHangfireServer();

        return services;
    }
}
