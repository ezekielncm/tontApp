namespace Domain.TontineManagement;

using Domain.Common;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;

public class Tontine : AggregateRoot<TontineId>
{
    private readonly List<Member> _members = [];
    private readonly List<Round> _rounds = [];
    private readonly List<Invitation> _invitations = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ContributionAmount ContributionAmount { get; private set; }
    public TontinePeriodicity Periodicity { get; private set; }
    public TontineStatus Status { get; private set; }
    public int MaxMembers { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public ModeAttribution ModeAttribution { get; private set; }

    public IReadOnlyCollection<Member> Members => _members.AsReadOnly();
    public IReadOnlyCollection<Round> Rounds => _rounds.AsReadOnly();
    public IReadOnlyCollection<Invitation> Invitations => _invitations.AsReadOnly();

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
        int maxMembers,
        ModeAttribution modeAttribution) : base(id)
    {
        Name = name;
        Description = description;
        ContributionAmount = contributionAmount;
        Periodicity = periodicity;
        Status = TontineStatus.Draft;
        MaxMembers = maxMembers;
        CreatedAt = DateTime.UtcNow;
        ModeAttribution = modeAttribution;
    }

    public static Tontine Create(
        string name,
        string? description,
        ContributionAmount contributionAmount,
        TontinePeriodicity periodicity,
        int maxMembers,
        ModeAttribution modeAttribution = ModeAttribution.Sequentiel)
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
            maxMembers,
            modeAttribution);

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

        var member = Member.Create(memberName, _members.Count + 1);
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

    public void Activate()
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Only a Draft tontine can be activated.");

        if (_members.Count < 2)
            throw new InvalidOperationException("A tontine must have at least 2 members to activate.");

        Status = TontineStatus.Active;
        StartedAt = DateTime.UtcNow;

        var firstBeneficiary = DetermineNextBeneficiary();
        var now = DateTime.UtcNow;
        var round = Round.Create(1, firstBeneficiary.Id, now, CalculateDeadline(now));
        _rounds.Add(round);

        AddDomainEvent(new TontineActivatedEvent(Id));
        AddDomainEvent(new RoundOpenedEvent(Id, round.Id, firstBeneficiary.Id, round.RoundNumber));
    }

    public Invitation GenerateInvitation(bool isMultipleUse = false)
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Invitations can only be generated when the tontine is in Draft status.");

        var invitation = Invitation.Create(isMultipleUse);
        _invitations.Add(invitation);

        AddDomainEvent(new InvitationGeneratedEvent(Id, invitation.Id, invitation.Code.ToString()));

        return invitation;
    }

    public Member JoinWithInvitation(string memberName, string invitationCode)
    {
        if (Status != TontineStatus.Draft)
            throw new InvalidOperationException("Members can only join when the tontine is in Draft status.");

        var invitation = _invitations.FirstOrDefault(i => i.Code.ToString() == invitationCode.ToUpperInvariant())
            ?? throw new InvalidOperationException("Invalid invitation code.");

        if (!invitation.IsValid())
            throw new InvalidOperationException("The invitation code is expired or already used.");

        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException($"Cannot add more than {MaxMembers} members.");

        if (_members.Any(m => m.Name == memberName))
            throw new InvalidOperationException($"A member with the name '{memberName}' already exists.");

        var member = Member.Create(memberName, _members.Count + 1);
        _members.Add(member);

        invitation.MarkUsed();

        AddDomainEvent(new MemberAddedEvent(Id, member.Id, memberName));

        return member;
    }

    public void SuspendMember(MemberId memberId)
    {
        if (Status != TontineStatus.Active)
            throw new InvalidOperationException("Members can only be suspended when the tontine is Active.");

        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        member.Suspendre();

        AddDomainEvent(new MemberSuspendedEvent(Id, memberId));
    }

    public void CloseRound(RoundId roundId)
    {
        if (Status != TontineStatus.Active)
            throw new InvalidOperationException("Rounds can only be closed when the tontine is Active.");

        var round = _rounds.FirstOrDefault(r => r.Id == roundId)
            ?? throw new InvalidOperationException("Round not found.");

        round.MarkCompleted();

        AddDomainEvent(new RoundClosedEvent(Id, round.Id, round.RoundNumber));

        var beneficiaryIds = _rounds.Select(r => r.BeneficiaryId).ToHashSet();
        var remainingMembers = _members
            .Where(m => m.Statut == StatutMembre.Actif && !beneficiaryIds.Contains(m.Id))
            .ToList();

        if (remainingMembers.Count > 0)
        {
            var nextBeneficiary = DetermineNextBeneficiaryFrom(remainingMembers);
            var now = DateTime.UtcNow;
            var nextRound = Round.Create(round.RoundNumber + 1, nextBeneficiary.Id, now, CalculateDeadline(now));
            _rounds.Add(nextRound);

            AddDomainEvent(new RoundOpenedEvent(Id, nextRound.Id, nextBeneficiary.Id, nextRound.RoundNumber));
        }
        else
        {
            Status = TontineStatus.Completed;
        }
    }

    public void Cancel()
    {
        if (Status == TontineStatus.Completed)
            throw new InvalidOperationException("A completed tontine cannot be cancelled.");

        if (Status == TontineStatus.Cancelled)
            throw new InvalidOperationException("The tontine is already cancelled.");

        Status = TontineStatus.Cancelled;
    }

    private Member DetermineNextBeneficiary()
    {
        var activeMembers = _members.Where(m => m.Statut == StatutMembre.Actif).ToList();

        if (activeMembers.Count == 0)
            throw new InvalidOperationException("No active members available to be beneficiary.");

        return DetermineNextBeneficiaryFrom(activeMembers);
    }

    private Member DetermineNextBeneficiaryFrom(List<Member> candidates)
    {
        if (candidates.Count == 0)
            throw new InvalidOperationException("No eligible members available to be beneficiary.");

        return ModeAttribution switch
        {
            ModeAttribution.Sequentiel => candidates.OrderBy(m => m.Rang).First(),
            ModeAttribution.Aleatoire => candidates[Random.Shared.Next(candidates.Count)],
            _ => throw new InvalidOperationException("Unknown attribution mode.")
        };
    }

    private DateTime CalculateDeadline(DateTime scheduledDate)
    {
        return Periodicity switch
        {
            TontinePeriodicity.Weekly => scheduledDate.AddDays(7),
            TontinePeriodicity.Biweekly => scheduledDate.AddDays(14),
            TontinePeriodicity.Monthly => scheduledDate.AddMonths(1),
            _ => scheduledDate.AddMonths(1)
        };
    }
}
