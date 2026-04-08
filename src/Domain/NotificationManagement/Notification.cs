namespace Domain.NotificationManagement;

using System.Text.RegularExpressions;
using Domain.Common;
using Domain.NotificationManagement.Events;
using Domain.NotificationManagement.ValueObjects;

public class Notification : AggregateRoot<NotificationId>
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    public string DestinataireId { get; private set; }
    public Canal Canal { get; private set; }
    public NotificationType Type { get; private set; }
    public ContenuMessage ContenuMessage { get; private set; }
    public string Contenu { get; private set; }
    public NotificationStatus Statut { get; private set; }
    public int TentativesEnvoi { get; private set; }
    public int MaxTentatives { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DateEnvoi { get; private set; }

    private Notification() : base()
    {
        DestinataireId = string.Empty;
        Contenu = string.Empty;
        ContenuMessage = null!;
    }

    private Notification(
        NotificationId id,
        string destinataireId,
        Canal canal,
        NotificationType type,
        ContenuMessage contenuMessage,
        int maxTentatives,
        DateTime? dateEnvoi) : base(id)
    {
        DestinataireId = destinataireId;
        Canal = canal;
        Type = type;
        ContenuMessage = contenuMessage;
        Contenu = contenuMessage.Texte;
        Statut = NotificationStatus.EnAttente;
        TentativesEnvoi = 0;
        MaxTentatives = maxTentatives;
        CreatedAt = DateTime.UtcNow;
        DateEnvoi = dateEnvoi;
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

        var contenuMsg = ContenuMessage.Create(contenu);

        var notification = new Notification(
            NotificationId.Create(),
            destinataireId,
            Canal.SMS,
            type,
            contenuMsg,
            maxTentatives,
            null);

        notification.AddDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            destinataireId,
            type));

        return notification;
    }

    /// <summary>
    /// Creates a notification with full options including canal and scheduled send date.
    /// Validates E.164 format for SMS destinations.
    /// </summary>
    public static Notification CreateFull(
        string destinataireId,
        Canal canal,
        NotificationType type,
        string contenu,
        int maxTentatives = 3,
        DateTime? dateEnvoi = null)
    {
        if (string.IsNullOrWhiteSpace(destinataireId))
            throw new ArgumentException("DestinataireId must not be empty.", nameof(destinataireId));

        if (canal == Canal.SMS && !E164Regex.IsMatch(destinataireId))
            throw new ArgumentException(
                "Le numéro de téléphone doit être au format E.164 (ex: +22670000000).",
                nameof(destinataireId));

        if (string.IsNullOrWhiteSpace(contenu))
            throw new ArgumentException("Contenu must not be empty.", nameof(contenu));

        var contenuMsg = ContenuMessage.Create(contenu);

        var notification = new Notification(
            NotificationId.Create(),
            destinataireId,
            canal,
            type,
            contenuMsg,
            maxTentatives,
            dateEnvoi);

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
        if (Statut == NotificationStatus.Envoyee)
            throw new InvalidOperationException("Cannot mark a sent notification as failed.");

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

    /// <summary>
    /// Returns true if this notification type is critical (payment confirmations)
    /// and should always be sent regardless of opt-out status.
    /// </summary>
    public bool EstCritique() =>
        Type == NotificationType.ConfirmationPaiement;
}
