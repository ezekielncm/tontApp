namespace Infrastructure.Persistence;

using System.Text.Json;
using Domain.BillingManagement;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.NotificationManagement;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Events;
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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

        // Outbox pattern: persist VersementConfirmedEvent in the same transaction
        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is VersementConfirmedEvent confirmedEvent)
            {
                var outboxMessage = OutboxMessage.Create(
                    nameof(VersementConfirmedEvent),
                    JsonSerializer.Serialize(new
                    {
                        confirmedEvent.VersementId.Value,
                        TontineId = confirmedEvent.TontineId.Value,
                        PayeurId = confirmedEvent.PayeurId.Value,
                        TourId = confirmedEvent.TourId.Value,
                        confirmedEvent.Montant,
                        confirmedEvent.ReferenceExterne,
                        confirmedEvent.OccurredOn
                    }));

                OutboxMessages.Add(outboxMessage);
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
