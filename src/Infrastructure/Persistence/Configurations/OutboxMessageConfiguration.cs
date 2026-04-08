namespace Infrastructure.Persistence.Configurations;

using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.TypeEvenement)
            .HasColumnName("type_evenement")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Contenu)
            .HasColumnName("contenu")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(o => o.Erreur)
            .HasColumnName("erreur");

        builder.Property(o => o.NombreTentatives)
            .HasColumnName("nombre_tentatives")
            .IsRequired()
            .HasDefaultValue(0);
    }
}
