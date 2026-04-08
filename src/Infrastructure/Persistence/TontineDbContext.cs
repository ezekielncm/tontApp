namespace Infrastructure.Persistence;

using Domain.BillingManagement;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.NotificationManagement;
using Domain.PaymentManagement;
using Domain.TontineManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class TontineDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator _mediator;

    public DbSet<Tontine> Tontines => Set<Tontine>();
    public DbSet<Versement> Versements => Set<Versement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Abonnement> Abonnements => Set<Abonnement>();

    public TontineDbContext(DbContextOptions<TontineDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TontineDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<AggregateRoot<object>>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .SelectMany(e =>
            {
                var events = e.Entity.DomainEvents.ToList();
                e.Entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
