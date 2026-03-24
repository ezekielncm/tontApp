namespace Domain.NotificationManagement;

using Domain.Common;
using Domain.NotificationManagement.Events;
using Domain.NotificationManagement.ValueObjects;

public class Notification : AggregateRoot<NotificationId>
{
    public string DestinataireId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Contenu { get; private set; }
    public NotificationStatus Statut { get; private set; }
    public int TentativesEnvoi { get; private set; }
    public int MaxTentatives { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private Notification() : base()
    {
        DestinataireId = string.Empty;
        Contenu = string.Empty;
    }

    private Notification(
        NotificationId id,
        string destinataireId,
        NotificationType type,
        string contenu,
        int maxTentatives) : base(id)
    {
        DestinataireId = destinataireId;
        Type = type;
        Contenu = contenu;
        Statut = NotificationStatus.EnAttente;
        TentativesEnvoi = 0;
        MaxTentatives = maxTentatives;
        CreatedAt = DateTime.UtcNow;
    }

    public static Notification Create(
        string destinataireId,
        NotificationType type,
        string contenu,
        int maxTentatives = 3)
    {
        if (string.IsNullOrWhiteSpace(destinataireId))
            throw new ArgumentException("DestinataireId must not be empty.", nameof(destinataireId));

        if (string.IsNullOrWhiteSpace(contenu))
            throw new ArgumentException("Contenu must not be empty.", nameof(contenu));

        var notification = new Notification(
            NotificationId.Create(),
            destinataireId,
            type,
            contenu,
            maxTentatives);

        notification.AddDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            destinataireId,
            type));

        return notification;
    }

    public void MarquerEnvoyee()
    {
        if (Statut == NotificationStatus.Envoyee)
            throw new InvalidOperationException("Notification is already sent.");

        Statut = NotificationStatus.Envoyee;
        SentAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationSentEvent(Id));
    }

    public bool MarquerEchouee()
    {
        TentativesEnvoi++;

        if (TentativesEnvoi >= MaxTentatives)
        {
            Statut = NotificationStatus.Echouee;
            return false;
        }

        return true;
    }

    public bool PeutReessayer() =>
        Statut != NotificationStatus.Envoyee &&
        Statut != NotificationStatus.Echouee &&
        TentativesEnvoi < MaxTentatives;
}
