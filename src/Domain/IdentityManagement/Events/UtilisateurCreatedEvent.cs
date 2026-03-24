namespace Domain.IdentityManagement.Events;

using Domain.Common;
using Domain.IdentityManagement.ValueObjects;

public sealed class UtilisateurCreatedEvent : IDomainEvent
{
    public UtilisateurId UtilisateurId { get; }
    public string Telephone { get; }
    public string Nom { get; }
    public DateTime OccurredOn { get; }

    public UtilisateurCreatedEvent(UtilisateurId utilisateurId, string telephone, string nom)
    {
        UtilisateurId = utilisateurId;
        Telephone = telephone;
        Nom = nom;
        OccurredOn = DateTime.UtcNow;
    }
}
