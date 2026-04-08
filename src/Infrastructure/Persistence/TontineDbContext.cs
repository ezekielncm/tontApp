namespace Infrastructure.Persistence;

using System.Text.Json;
using Domain.BillingManagement;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.NotificationManagement;
using Domain.NotificationManagement.Events;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Events;
using Domain.TontineManagement;
using Domain.TontineManagement.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class TontineDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator _mediator;

    public DbSet<Tontine> Tontines => Set<Tontine>();
    public DbSet<Versement> Versements => Set<Versement>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Abonnement> Abonnements => Set<Abonnement>();
    public DbSet<PlanAbonnement> PlansAbonnement => Set<PlanAbonnement>();
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

        // Outbox pattern: persist domain events in the same transaction
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
            else if (domainEvent is RoundOpenedEvent roundOpenedEvent)
            {
                var outboxMessage = OutboxMessage.Create(
                    nameof(RoundOpenedEvent),
                    JsonSerializer.Serialize(new
                    {
                        TontineId = roundOpenedEvent.TontineId.Value,
                        RoundId = roundOpenedEvent.RoundId.Value,
                        BeneficiaryId = roundOpenedEvent.BeneficiaryId.Value,
                        roundOpenedEvent.RoundNumber,
                        roundOpenedEvent.OccurredOn
                    }));

                OutboxMessages.Add(outboxMessage);
            }
            else if (domainEvent is PaiementEnRetardEvent retardEvent)
            {
                var outboxMessage = OutboxMessage.Create(
                    nameof(PaiementEnRetardEvent),
                    JsonSerializer.Serialize(new
                    {
                        TontineId = retardEvent.TontineId.Value,
                        TourId = retardEvent.TourId.Value,
                        PayeurId = retardEvent.PayeurId.Value,
                        retardEvent.Montant,
                        retardEvent.Devise,
                        retardEvent.OccurredOn
                    }));

                OutboxMessages.Add(outboxMessage);
            }
            else if (domainEvent is NotificationCreatedEvent notificationCreatedEvent)
            {
                var outboxMessage = OutboxMessage.Create(
                    nameof(NotificationCreatedEvent),
                    JsonSerializer.Serialize(new
                    {
                        NotificationId = notificationCreatedEvent.NotificationId.Value,
                        notificationCreatedEvent.DestinataireId,
                        Type = notificationCreatedEvent.Type.ToString(),
                        notificationCreatedEvent.OccurredOn
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
