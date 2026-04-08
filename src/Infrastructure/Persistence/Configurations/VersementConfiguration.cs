namespace Infrastructure.Persistence.Configurations;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class VersementConfiguration : IEntityTypeConfiguration<Versement>
{
    public void Configure(EntityTypeBuilder<Versement> builder)
    {
        builder.ToTable("versements");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => VersementId.From(value));

        builder.Property(v => v.TontineId)
            .HasColumnName("tontine_id")
            .HasConversion(
                id => id.Value,
                value => TontineId.From(value))
            .IsRequired();

        builder.Property(v => v.TourId)
            .HasColumnName("tour_id")
            .HasConversion(
                id => id.Value,
                value => TourId.From(value))
            .IsRequired();

        builder.Property(v => v.PayeurId)
            .HasColumnName("membre_id")
            .HasConversion(
                id => id.Value,
                value => PayeurId.From(value))
            .IsRequired();

        // Montant as owned value object - uses NUMERIC(15,2), never float/double
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

        // Audit trail navigation (private backing field)
        builder.HasMany<AuditEntry>("_auditTrail")
            .WithOne()
            .HasForeignKey("versement_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_auditTrail").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(v => v.DomainEvents);

        // Indexes
        builder.HasIndex(v => new { TontineId = v.TontineId, TourId = v.TourId })
            .HasDatabaseName("ix_versements_tontine_tour");

        builder.HasIndex(v => v.ReferenceExterne)
            .IsUnique()
            .HasDatabaseName("uq_versements_reference_externe")
            .HasFilter("reference_externe IS NOT NULL");
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => AuditEntryId.From(value));

        builder.Property(e => e.PreviousHash)
            .HasColumnName("hash_precedent")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Hash)
            .HasColumnName("hash_courant")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("horodatage")
            .IsRequired();

        builder.Property(e => e.ActorId)
            .HasColumnName("acteur_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(e => new { e.Timestamp })
            .HasDatabaseName("ix_audit_entries_versement");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("ix_audit_entries_action");
    }
}
