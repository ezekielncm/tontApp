using System.Text;
using System.Threading.RateLimiting;
using Application;
using Hangfire;
using Infrastructure;
using Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        // ── Serilog bootstrap ──────────────────────────────────────────
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Serilog ────────────────────────────────────────────────
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq(context.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341"));

            // ── Application layer (MediatR + behaviors) ────────────────
            builder.Services.AddApplication();

            // ── Infrastructure layer (EF Core, Hangfire, Redis, repos) ─
            builder.Services.AddInfrastructure(builder.Configuration);

            // ── JWT Authentication ─────────────────────────────────────
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    var jwtSection = builder.Configuration.GetSection("Jwt");
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.")))
                    };
                });

            builder.Services.AddAuthorization();

            // ── Rate Limiting ──────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        }));

                options.AddPolicy("sliding", httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        }));
            });

            // ── Controllers & OpenAPI ──────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // ── Health checks ──────────────────────────────────────────
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // ── HTTP pipeline ──────────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new Infrastructure.Jobs.HangfireDashboardAuthFilter()],
                IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()
            });

            // Register daily audit chain verification job (runs at 02:00 UTC every day)
            RecurringJob.AddOrUpdate<VerifierChaineAuditJob>(
                "verifier-chaine-audit-quotidien",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(2, 0));

            // OutboxProcessor: every 30 seconds
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.OutboxProcessor>(
                "outbox-processor",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/30 * * * * *");

            // RappelJ3: daily at 08:00 UTC
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.RappelJ3Job>(
                "rappel-j3-quotidien",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(8, 0));

            // RappelJ1: daily at 08:00 UTC
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.RappelJ1Job>(
                "rappel-j1-quotidien",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(8, 0));

            // RecapHebdo: every Monday at 09:00 UTC
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.RecapHebdoJob>(
                "recap-hebdomadaire",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Weekly(DayOfWeek.Monday, 9, 0));

            // RappelRenouvellementJ3: daily at 08:00 UTC (3 days before subscription expiry)
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.RappelRenouvellementJ3Job>(
                "rappel-renouvellement-j3",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(8, 0));

            // RenouvellementAbonnement: daily at 00:30 UTC (attempt auto-renewal on expiry day)
            RecurringJob.AddOrUpdate<Infrastructure.Jobs.RenouvellementAbonnementJob>(
                "renouvellement-abonnement-quotidien",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(0, 30));

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
