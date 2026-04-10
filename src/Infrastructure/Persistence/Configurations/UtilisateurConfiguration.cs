namespace Infrastructure.Persistence.Configurations;

using Domain.IdentityManagement;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("utilisateurs");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => UtilisateurId.From(value));

        builder.Property(u => u.Telephone)
            .HasColumnName("telephone")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                tel => tel.Value,
                value => TelephoneId.From(value));

        builder.HasIndex(u => u.Telephone).IsUnique();

        builder.Property(u => u.Nom)
            .HasColumnName("nom")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.MotDePasseHash)
            .HasColumnName("mot_de_passe_hash")
            .HasMaxLength(256)
            .IsRequired()
            .HasConversion(
                hash => hash.Value,
                value => MotDePasseHash.FromHash(value));

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                role => role.ToString().ToUpperInvariant(),
                value => Enum.Parse<RoleUtilisateur>(value, ignoreCase: true));

        builder.Property(u => u.EstActif)
            .HasColumnName("est_actif")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.FcmToken)
            .HasColumnName("fcm_token")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(u => u.SmsOptOut)
            .HasColumnName("sms_opt_out")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Ignore(u => u.DomainEvents);
    }
}
