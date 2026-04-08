namespace Infrastructure;

using Application.IdentityManagement.Services;
using Application.PaymentManagement.Services;
using Domain.BillingManagement.Repositories;
using Domain.Common;
using Domain.IdentityManagement.Repositories;
using Domain.NotificationManagement.Repositories;
using Domain.PaymentManagement.Ports;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Auth;
using Infrastructure.Jobs;
using Infrastructure.Payment;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUtilisateurRepository, UtilisateurRepository>();
        services.AddScoped<IAbonnementRepository, AbonnementRepository>();

        // Audit trail service
        services.AddScoped<IAuditTrailService, AuditTrailService>();

        // Hangfire jobs
        services.AddScoped<VerifierChaineAuditJob>();

        // Africa's Talking / Orange Money configuration
        services.Configure<AfricasTalkingOptions>(
            configuration.GetSection(AfricasTalkingOptions.SectionName));

        services.AddHttpClient<IMobileMoneyGateway, OrangeMoneyAdapter>((sp, client) =>
        {
            var options = configuration.GetSection(AfricasTalkingOptions.SectionName)
                .Get<AfricasTalkingOptions>() ?? new AfricasTalkingOptions();

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("apiKey", options.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Redis – connection multiplexer (singleton) + distributed cache
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "TontinesApp:";
        });

        // Auth services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();

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
