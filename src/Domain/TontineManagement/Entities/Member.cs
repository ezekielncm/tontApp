namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public class Member : Entity<MemberId>
{
    public string Name { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public int Rang { get; private set; }
    public StatutMembre Statut { get; private set; }

    /// <summary>
    /// Optional link to the authenticated user who is this member.
    /// Null when the member was added manually by the gestionnaire (without a user account).
    /// </summary>
    public UtilisateurId? UtilisateurId { get; private set; }

    private Member() : base()
    {
        Name = string.Empty;
    }

    internal Member(MemberId id, string name, DateTime joinedAt, int rang, UtilisateurId? utilisateurId = null) : base(id)
    {
        Name = name;
        JoinedAt = joinedAt;
        Rang = rang;
        Statut = StatutMembre.Actif;
        UtilisateurId = utilisateurId;
    }

    public static Member Create(string name, int rang = 0, UtilisateurId? utilisateurId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Member name must not be empty.", nameof(name));

        return new Member(MemberId.Create(), name, DateTime.UtcNow, rang, utilisateurId);
    }

    public void Suspendre()
    {
        Statut = StatutMembre.Suspendu;
    }

    public void Reactiver()
    {
        Statut = StatutMembre.Actif;
    }
}
