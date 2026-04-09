namespace PaymentIntegrationTests;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Domain.Common;

/// <summary>
/// A test-specific DbContext that only includes Payment-related entities
/// (Versement, AuditEntry), avoiding the full model complexity.
/// Used exclusively for integration tests with TestContainers.
/// </summary>
internal sealed class PaymentTestDbContext : DbContext
{
    private readonly IMediator _mediator;

    public DbSet<Versement> Versements => Set<Versement>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public PaymentTestDbContext(DbContextOptions<PaymentTestDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureVersement(modelBuilder);
        ConfigureAuditEntry(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureVersement(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Versement>();

        builder.ToTable("versements");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => VersementId.From(value));

        builder.Property(v => v.TontineId)
            .HasColumnName("tontine_id")
            .HasConversion(id => id.Value, value => TontineId.From(value))
            .IsRequired();

        builder.Property(v => v.TourId)
            .HasColumnName("tour_id")
            .HasConversion(id => id.Value, value => TourId.From(value))
            .IsRequired();

        builder.Property(v => v.PayeurId)
            .HasColumnName("membre_id")
            .HasConversion(id => id.Value, value => PayeurId.From(value))
            .IsRequired();

        builder.OwnsOne(v => v.Montant, mb =>
        {
            mb.Property(m => m.Valeur)
                .HasColumnName("montant")
                .HasColumnType("numeric(15,2)")
                .IsRequired();
            mb.Property(m => m.Devise)
                .HasColumnName("devise")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(v => v.Statut)
            .HasColumnName("statut")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                value => Enum.Parse<VersementStatus>(value, ignoreCase: true));

        builder.Property(v => v.ReferenceExterne)
            .HasColumnName("reference_externe")
            .HasMaxLength(100);

        builder.Property(v => v.HashPrecedent)
            .HasColumnName("hash_precedent")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(v => v.HashCourant)
            .HasColumnName("hash_courant")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(v => v.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.HasMany<AuditEntry>("AuditTrail")
            .WithOne()
            .HasForeignKey(e => e.VersementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("AuditTrail")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_auditTrail");

        builder.Ignore(v => v.DomainEvents);

        builder.HasIndex(v => new { v.TontineId, v.TourId })
            .HasDatabaseName("ix_versements_tontine_tour");

        builder.HasIndex(v => v.ReferenceExterne)
            .IsUnique()
            .HasDatabaseName("uq_versements_reference_externe")
            .HasFilter("reference_externe IS NOT NULL");
    }

    private static void ConfigureAuditEntry(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<AuditEntry>();

        builder.ToTable("audit_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => AuditEntryId.From(value));

        builder.Property(e => e.VersementId)
            .HasColumnName("versement_id")
            .HasConversion(id => id.Value, value => VersementId.From(value))
            .IsRequired();

        builder.Property(e => e.TontineId)
            .HasColumnName("tontine_id")
            .HasConversion(id => id.Value, value => TontineId.From(value))
            .IsRequired();

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(
                a => a.ToString(),
                value => Enum.Parse<AuditAction>(value, ignoreCase: true));

        builder.Property(e => e.ActeurId)
            .HasColumnName("acteur_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("horodatage")
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.HashPrecedent)
            .HasColumnName("hash_precedent")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.HashCourant)
            .HasColumnName("hash_courant")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(e => new { e.TontineId, e.Timestamp })
            .HasDatabaseName("ix_audit_entries_tontine_horodatage")
            .IsDescending(false, true);

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("ix_audit_entries_action");

        builder.HasIndex(e => e.ActeurId)
            .HasDatabaseName("ix_audit_entries_acteur");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Clear domain events (simplified - no outbox for test context)
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
