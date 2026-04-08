namespace Infrastructure.Persistence.Configurations;

using Domain.NotificationManagement;
using Domain.NotificationManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => NotificationId.From(value));

        builder.Property(n => n.DestinataireId)
            .HasColumnName("destinataire_id")
            .IsRequired();

        builder.Property(n => n.Canal)
            .HasColumnName("canal")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion(
                c => c.ToString().ToUpperInvariant(),
                value => Enum.Parse<Canal>(value, ignoreCase: true));

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(
                t => t.ToString().ToUpperInvariant(),
                value => Enum.Parse<NotificationType>(value, ignoreCase: true));

        builder.OwnsOne(n => n.ContenuMessage, cmb =>
        {
            cmb.Property(cm => cm.Texte)
                .HasColumnName("contenu_message")
                .HasMaxLength(ContenuMessage.MaxLength)
                .IsRequired();
        });

        builder.Property(n => n.Contenu)
            .HasColumnName("contenu")
            .IsRequired();

        builder.Property(n => n.Statut)
            .HasColumnName("statut")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                value => Enum.Parse<NotificationStatus>(value, ignoreCase: true));

        builder.Property(n => n.TentativesEnvoi)
            .HasColumnName("tentatives_envoi")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.MaxTentatives)
            .HasColumnName("max_tentatives")
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.SentAt)
            .HasColumnName("sent_at");

        builder.Property(n => n.DateEnvoi)
            .HasColumnName("date_envoi");

        builder.Ignore(n => n.DomainEvents);
    }
}
