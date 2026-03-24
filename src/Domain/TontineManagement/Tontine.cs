namespace Domain.TontineManagement;

using Domain.Common;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;

public class Tontine : AggregateRoot<TontineId>
{
    private readonly List<Member> _members = [];
    private readonly List<Round> _rounds = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ContributionAmount ContributionAmount { get; private set; }
    public TontinePeriodicity Periodicity { get; private set; }
    public TontineStatus Status { get; private set; }
    public int MaxMembers { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }

    public IReadOnlyCollection<Member> Members => _members.AsReadOnly();
    public IReadOnlyCollection<Round> Rounds => _rounds.AsReadOnly();

    private Tontine() : base()
    {
        Name = string.Empty;
        ContributionAmount = default!;
    }

    private Tontine(
        TontineId id,
        string name,
        string? description,
        ContributionAmount contributionAmount,
        TontinePeriodicity periodicity,
        int maxMembers) : base(id)
    {
        Name = name;
        Description = description;
        ContributionAmount = contributionAmount;
        Periodicity = periodicity;
        Status = TontineStatus.Draft;
        MaxMembers = maxMembers;
        CreatedAt = DateTime.UtcNow;
    }

    public static Tontine Create(
        string name,
        string? description,
        ContributionAmount contributionAmount,
        TontinePeriodicity periodicity,
        int maxMembers)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tontine name must not be empty.", nameof(name));

        if (maxMembers < 2)
            throw new ArgumentException("A tontine must have at least 2 members.", nameof(maxMembers));

        var tontine = new Tontine(
            TontineId.Create(),
            name,
            description,
            contributionAmount,
            periodicity,
            maxMembers);

        tontine.AddDomainEvent(new TontineCreatedEvent(tontine.Id, name));

        return tontine;
    }

    public Member AddMember(string memberName)
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Members can only be added when the tontine is in Draft status.");

        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException($"Cannot add more than {MaxMembers} members.");

        if (_members.Any(m => m.Name == memberName))
            throw new InvalidOperationException($"A member with the name '{memberName}' already exists.");

        var member = Member.Create(memberName);
        _members.Add(member);

        AddDomainEvent(new MemberAddedEvent(Id, member.Id, memberName));

        return member;
    }

    public void RemoveMember(MemberId memberId)
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Members can only be removed when the tontine is in Draft status.");

        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        _members.Remove(member);

        AddDomainEvent(new MemberRemovedEvent(Id, memberId));
    }

    public void Start()
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Only a Draft tontine can be started.");

        if (_members.Count < 2)
            throw new InvalidOperationException("A tontine must have at least 2 members to start.");

        Status = TontineStatus.Active;
        StartedAt = DateTime.UtcNow;

        AddDomainEvent(new TontineStartedEvent(Id));
    }

    public void Cancel()
    {
        if (Status == TontineStatus.Completed)
            throw new InvalidOperationException("A completed tontine cannot be cancelled.");

        if (Status == TontineStatus.Cancelled)
            throw new InvalidOperationException("The tontine is already cancelled.");

        Status = TontineStatus.Cancelled;
    }
}
