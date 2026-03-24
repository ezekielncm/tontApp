namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public class Member : Entity<MemberId>
{
    public string Name { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private Member() : base()
    {
        Name = string.Empty;
    }

    internal Member(MemberId id, string name, DateTime joinedAt) : base(id)
    {
        Name = name;
        JoinedAt = joinedAt;
    }

    public static Member Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Member name must not be empty.", nameof(name));

        return new Member(MemberId.Create(), name, DateTime.UtcNow);
    }
}
