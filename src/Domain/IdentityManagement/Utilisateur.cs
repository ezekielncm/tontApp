namespace Domain.IdentityManagement;

using Domain.Common;
using Domain.IdentityManagement.Events;
using Domain.IdentityManagement.ValueObjects;

public class Utilisateur : AggregateRoot<UtilisateurId>
{
    public TelephoneId Telephone { get; private set; } = null!;
    public string Nom { get; private set; }
    public MotDePasseHash MotDePasseHash { get; private set; } = null!;
    public RoleUtilisateur Role { get; private set; }
    public bool EstActif { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Utilisateur() : base()
    {
        Nom = string.Empty;
    }

    private Utilisateur(
        UtilisateurId id,
        TelephoneId telephone,
        string nom,
        MotDePasseHash motDePasseHash,
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
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Nom must not be empty.", nameof(nom));

        var telephoneId = TelephoneId.Create(telephone);
        var hash = MotDePasseHash.FromHash(motDePasseHash);

        var utilisateur = new Utilisateur(
            UtilisateurId.Create(),
            telephoneId,
            nom,
            hash,
            role);

        utilisateur.AddDomainEvent(new UtilisateurInscritEvent(
            utilisateur.Id,
            telephoneId.Value,
            nom));

        return utilisateur;
    }

    public void Desactiver() => EstActif = false;

    public void Activer() => EstActif = true;

    public void ChangerRole(RoleUtilisateur nouveauRole) => Role = nouveauRole;
}
