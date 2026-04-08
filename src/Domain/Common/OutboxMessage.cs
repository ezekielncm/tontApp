namespace Domain.Common;

/// <summary>
/// Represents a domain event persisted in the outbox table for reliable event publishing.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string TypeEvenement { get; private set; }
    public string Contenu { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Erreur { get; private set; }
    public int NombreTentatives { get; private set; }

    private OutboxMessage()
    {
        TypeEvenement = string.Empty;
        Contenu = string.Empty;
    }

    public static OutboxMessage Create(string typeEvenement, string contenu)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TypeEvenement = typeEvenement,
            Contenu = contenu,
            CreatedAt = DateTime.UtcNow,
            NombreTentatives = 0
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string erreur)
    {
        Erreur = erreur;
        NombreTentatives++;
    }
}
