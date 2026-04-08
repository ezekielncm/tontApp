namespace Infrastructure.Persistence.Configurations;

using Domain.BillingManagement;
using Domain.BillingManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PlanAbonnementConfiguration : IEntityTypeConfiguration<PlanAbonnement>
{
    public void Configure(EntityTypeBuilder<PlanAbonnement> builder)
    {
        builder.ToTable("plans_abonnement");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PlanAbonnementId.From(value));

        builder.Property(p => p.Nom)
            .HasColumnName("nom")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PrixMensuel)
            .HasColumnName("prix_mensuel")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(p => p.Devise)
            .HasColumnName("devise")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.MaxTontines)
            .HasColumnName("max_tontines")
            .IsRequired();

        builder.Property(p => p.MaxMembresParTontine)
            .HasColumnName("max_membres_par_tontine")
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.EstActif)
            .HasColumnName("est_actif")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("uq_plans_abonnement_code");

        // Seed data
        builder.HasData(
            PlanAbonnement.CreateWithId(
                PlanAbonnement.SeedIds.Gratuit,
                "Gratuit",
                PlanAbonnement.Codes.Gratuit,
                0m,
                "XOF",
                maxTontines: 1,
                maxMembresParTontine: 10,
                "Plan gratuit : 1 tontine, 10 membres max"),
            PlanAbonnement.CreateWithId(
                PlanAbonnement.SeedIds.Pro,
                "Pro",
                PlanAbonnement.Codes.Pro,
                2000m,
                "XOF",
                maxTontines: 10,
                maxMembresParTontine: int.MaxValue,
                "Plan Pro : 10 tontines, membres illimités - 2000 FCFA/mois"),
            PlanAbonnement.CreateWithId(
                PlanAbonnement.SeedIds.Imf,
                "IMF",
                PlanAbonnement.Codes.Imf,
                0m,
                "XOF",
                maxTontines: int.MaxValue,
                maxMembresParTontine: int.MaxValue,
                "Plan IMF : sur devis, tontines et membres illimités"));
    }
}

internal sealed class AbonnementConfiguration : IEntityTypeConfiguration<Abonnement>
{
    public void Configure(EntityTypeBuilder<Abonnement> builder)
    {
        builder.ToTable("abonnements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => AbonnementId.From(value));

        builder.Property(a => a.GestionnaireId)
            .HasColumnName("gestionnaire_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.PlanId)
            .HasColumnName("plan_id")
            .HasConversion(
                id => id.Value,
                value => PlanAbonnementId.From(value))
            .IsRequired();

        builder.Property(a => a.Plan)
            .HasColumnName("plan")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                p => p.ToString().ToUpperInvariant(),
                value => Enum.Parse<PlanTarifaire>(value, ignoreCase: true));

        builder.Property(a => a.Statut)
            .HasColumnName("statut")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                value => Enum.Parse<StatutAbonnement>(value, ignoreCase: true));

        builder.Property(a => a.MontantMensuel)
            .HasColumnName("montant_mensuel")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasColumnName("devise")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.DateDebut)
            .HasColumnName("date_debut")
            .IsRequired();

        builder.Property(a => a.DateFin)
            .HasColumnName("date_fin")
            .IsRequired();

        builder.Property(a => a.DateFinGrace)
            .HasColumnName("date_fin_grace");

        builder.Property(a => a.RenouvellementAuto)
            .HasColumnName("renouvellement_auto")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.DernierTransactionId)
            .HasColumnName("dernier_transaction_id")
            .HasMaxLength(100);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Ignore(a => a.DomainEvents);

        // Foreign key to PlanAbonnement
        builder.HasOne<PlanAbonnement>()
            .WithMany()
            .HasForeignKey(a => a.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.GestionnaireId)
            .HasDatabaseName("ix_abonnements_gestionnaire");

        builder.HasIndex(a => a.Statut)
            .HasDatabaseName("ix_abonnements_statut");

        builder.HasIndex(a => a.DateFin)
            .HasDatabaseName("ix_abonnements_date_fin");
    }
}
