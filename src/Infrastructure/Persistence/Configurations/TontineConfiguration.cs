namespace Infrastructure.Persistence.Configurations;

using Domain.TontineManagement;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TontineConfiguration : IEntityTypeConfiguration<Tontine>
{
    public void Configure(EntityTypeBuilder<Tontine> builder)
    {
        builder.ToTable("tontines");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => TontineId.From(value));

        builder.Property(t => t.Name)
            .HasColumnName("nom")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description");

        builder.Property(t => t.Status)
            .HasColumnName("statut")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                value => Enum.Parse<TontineStatus>(value, ignoreCase: true));

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.StartedAt)
            .HasColumnName("started_at");

        builder.OwnsOne(t => t.Reglement, rb =>
        {
            rb.OwnsOne(r => r.ContributionAmount, cab =>
            {
                cab.Property(c => c.Amount)
                    .HasColumnName("montant_cotisation")
                    .HasColumnType("numeric(15,2)")
                    .IsRequired();

                cab.Property(c => c.Currency)
                    .HasColumnName("devise")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            rb.Property(r => r.Periodicity)
                .HasColumnName("periodicite")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    p => p.ToString().ToUpperInvariant(),
                    value => Enum.Parse<TontinePeriodicity>(value, ignoreCase: true));

            rb.Property(r => r.MaxMembers)
                .HasColumnName("max_membres")
                .IsRequired();

            rb.Property(r => r.ModeAttribution)
                .HasColumnName("mode_attribution")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    m => m.ToString().ToUpperInvariant(),
                    value => Enum.Parse<ModeAttribution>(value, ignoreCase: true));

            rb.Property(r => r.MinMembresActivation)
                .HasColumnName("min_membres_activation")
                .IsRequired()
                .HasDefaultValue(3);
        });

        // Members navigation
        builder.HasMany<Member>("_members")
            .WithOne()
            .HasForeignKey("tontine_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_members").UsePropertyAccessMode(PropertyAccessMode.Field);

        // Rounds navigation
        builder.HasMany<Round>("_rounds")
            .WithOne()
            .HasForeignKey("tontine_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_rounds").UsePropertyAccessMode(PropertyAccessMode.Field);

        // Invitations navigation
        builder.HasMany<Invitation>("_invitations")
            .WithOne()
            .HasForeignKey("tontine_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_invitations").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);

        // Computed properties sourced from Reglement
        builder.Ignore(t => t.ContributionAmount);
        builder.Ignore(t => t.Periodicity);
        builder.Ignore(t => t.MaxMembers);
        builder.Ignore(t => t.ModeAttribution);
    }
}

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("membres_tontine");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => MemberId.From(value));

        builder.Property(m => m.Name)
            .HasColumnName("nom")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Rang)
            .HasColumnName("rang")
            .IsRequired();

        builder.Property(m => m.Statut)
            .HasColumnName("statut")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                value => Enum.Parse<StatutMembre>(value, ignoreCase: true));

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();
    }
}

internal sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("tours_de_role");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => RoundId.From(value));

        builder.Property(r => r.RoundNumber)
            .HasColumnName("numero_tour")
            .IsRequired();

        builder.Property(r => r.BeneficiaryId)
            .HasColumnName("beneficiaire_id")
            .HasConversion(
                id => id.Value,
                value => MemberId.From(value))
            .IsRequired();

        builder.Property(r => r.ScheduledDate)
            .HasColumnName("date_prevue")
            .IsRequired();

        builder.Property(r => r.DateLimite)
            .HasColumnName("date_limite")
            .IsRequired();

        builder.Property(r => r.IsCompleted)
            .HasColumnName("est_complete")
            .IsRequired();
    }
}
