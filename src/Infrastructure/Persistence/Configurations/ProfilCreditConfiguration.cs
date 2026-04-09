namespace Infrastructure.Persistence.Configurations;

using Domain.CreditScoringManagement;
using Domain.CreditScoringManagement.Entities;
using Domain.CreditScoringManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ProfilCreditConfiguration : IEntityTypeConfiguration<ProfilCredit>
{
    public void Configure(EntityTypeBuilder<ProfilCredit> builder)
    {
        builder.ToTable("profils_credit");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => ProfilCreditId.From(value));

        builder.Property(p => p.MembreId)
            .HasColumnName("membre_id")
            .IsRequired();

        builder.Property(p => p.DonneesInsuffisantes)
            .HasColumnName("donnees_insuffisantes")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // ScoreCalcule as owned value object
        builder.OwnsOne(p => p.ScoreActuel, sb =>
        {
            sb.Property(s => s.Valeur)
                .HasColumnName("score_valeur")
                .IsRequired();

            sb.Property(s => s.CyclesCompletes)
                .HasColumnName("score_cycles_completes")
                .IsRequired();

            sb.Property(s => s.TauxPonctualite)
                .HasColumnName("score_taux_ponctualite")
                .IsRequired();

            sb.Property(s => s.AncienneteEnMois)
                .HasColumnName("score_anciennete_mois")
                .IsRequired();

            sb.Property(s => s.Niveau)
                .HasColumnName("score_niveau")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    n => n.ToString(),
                    value => Enum.Parse<NiveauRisque>(value, ignoreCase: true));

            sb.Property(s => s.CalculeLe)
                .HasColumnName("score_calcule_le")
                .IsRequired();
        });

        // HistoriqueComportement as owned entity
        builder.OwnsOne(p => p.Historique, hb =>
        {
            hb.Property(h => h.Id)
                .HasColumnName("historique_id")
                .HasConversion(
                    id => id.Value,
                    value => HistoriqueComportementId.From(value));

            hb.Property(h => h.TotalVersements)
                .HasColumnName("historique_total_versements")
                .IsRequired();

            hb.Property(h => h.VersementsPonctuels)
                .HasColumnName("historique_versements_ponctuels")
                .IsRequired();

            hb.Property(h => h.CyclesCompletes)
                .HasColumnName("historique_cycles_completes")
                .IsRequired();

            hb.Property(h => h.DatePremierVersement)
                .HasColumnName("historique_date_premier_versement")
                .IsRequired();

            hb.Property(h => h.DernierVersement)
                .HasColumnName("historique_dernier_versement")
                .IsRequired();
        });

        builder.Ignore(p => p.DomainEvents);

        // Indexes
        builder.HasIndex(p => p.MembreId)
            .IsUnique()
            .HasDatabaseName("uq_profils_credit_membre");
    }
}
