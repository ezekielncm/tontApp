namespace Domain.IdentityManagement;

using Domain.Common;
using Domain.IdentityManagement.Events;
using Domain.IdentityManagement.ValueObjects;

public class Utilisateur : AggregateRoot<UtilisateurId>
{
    public string Telephone { get; private set; }
    public string Nom { get; private set; }
    public string MotDePasseHash { get; private set; }
    public RoleUtilisateur Role { get; private set; }
    public bool EstActif { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Utilisateur() : base()
    {
        Telephone = string.Empty;
        Nom = string.Empty;
        MotDePasseHash = string.Empty;
    }

    private Utilisateur(
        UtilisateurId id,
        string telephone,
        string nom,
        string motDePasseHash,
        RoleUtilisateur role) : base(id)
    {
        Telephone = telephone;
        Nom = nom;
        MotDePasseHash = motDePasseHash;
        Role = role;
        EstActif = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Utilisateur Create(
        string telephone,
        string nom,
        string motDePasseHash,
        RoleUtilisateur role = RoleUtilisateur.Membre)
    {
        if (string.IsNullOrWhiteSpace(telephone))
            throw new ArgumentException("Telephone must not be empty.", nameof(telephone));

        if (!telephone.StartsWith('+') || telephone.Length < 4)
            throw new ArgumentException("Telephone must start with '+' and contain at least a country code.", nameof(telephone));

        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Nom must not be empty.", nameof(nom));

        if (string.IsNullOrWhiteSpace(motDePasseHash))
            throw new ArgumentException("MotDePasseHash must not be empty.", nameof(motDePasseHash));

        var utilisateur = new Utilisateur(
            UtilisateurId.Create(),
            telephone,
            nom,
            motDePasseHash,
            role);

        utilisateur.AddDomainEvent(new UtilisateurCreatedEvent(
            utilisateur.Id,
            telephone,
            nom));

        return utilisateur;
    }

    public void Desactiver() => EstActif = false;

    public void Activer() => EstActif = true;

    public void ChangerRole(RoleUtilisateur nouveauRole) => Role = nouveauRole;
}
