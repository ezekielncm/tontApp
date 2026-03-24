namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public class Member : Entity<MemberId>
{
    public string Name { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public int Rang { get; private set; }
    public StatutMembre Statut { get; private set; }

    private Member() : base()
    {
        Name = string.Empty;
    }

    internal Member(MemberId id, string name, DateTime joinedAt, int rang) : base(id)
    {
        Name = name;
        JoinedAt = joinedAt;
        Rang = rang;
        Statut = StatutMembre.Actif;
    }

    public static Member Create(string name, int rang = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Member name must not be empty.", nameof(name));

        return new Member(MemberId.Create(), name, DateTime.UtcNow, rang);
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
