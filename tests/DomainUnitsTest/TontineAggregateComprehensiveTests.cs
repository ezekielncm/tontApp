using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;
using FluentAssertions;

namespace DomainUnitsTest;

public class TontineAggregateComprehensiveTests
{
    // ── Helpers ─────────────────────────────────────────────────────

    private static readonly UtilisateurId TestGestionnaireId = UtilisateurId.Create();

    private static Tontine CreateDraftTontine(
        string name = "Test Tontine",
        string? description = "A test tontine",
        decimal amount = 5000m,
        string currency = "XOF",
        int maxMembers = 5,
        ModeAttribution mode = ModeAttribution.Sequentiel)
    {
        var contribution = ContributionAmount.Create(amount, currency);
        return Tontine.Create(name, description, contribution, TontinePeriodicity.Monthly, maxMembers, TestGestionnaireId, mode);
    }

    private static Tontine CreateActiveTontine(int memberCount = 3, int maxMembers = 5, ModeAttribution mode = ModeAttribution.Sequentiel)
    {
        var tontine = CreateDraftTontine(maxMembers: Math.Max(memberCount, maxMembers), mode: mode);
        for (var i = 1; i <= memberCount; i++)
            tontine.AddMember($"Member{i}");
        tontine.Activate();
        return tontine;
    }

    private static Tontine CreateSuspendedTontine(int memberCount = 3)
    {
        var tontine = CreateActiveTontine(memberCount);
        tontine.Suspendre();
        return tontine;
    }

    private static Tontine CreateCompletedTontine()
    {
        var tontine = CreateActiveTontine(3, maxMembers: 3);
        // Close all rounds to complete the tontine
        while (tontine.Status == TontineStatus.Active)
        {
            var openRound = tontine.Rounds.First(r => !r.IsCompleted);
            tontine.CloseRound(openRound.Id);
        }
        return tontine;
    }

    private static Tontine CreateCancelledTontine()
    {
        var tontine = CreateDraftTontine();
        tontine.Cancel();
        return tontine;
    }

    // ── 1-4: Create edge cases ──────────────────────────────────────

    [Fact]
    public void Create_WithNullDescription_Succeeds()
    {
        var tontine = CreateDraftTontine(description: null);

        tontine.Description.Should().BeNull();
        tontine.Status.Should().Be(TontineStatus.Draft);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        var tontine = CreateDraftTontine(description: "Ma tontine familiale");

        tontine.Description.Should().Be("Ma tontine familiale");
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var tontine = CreateDraftTontine();
        var after = DateTime.UtcNow;

        tontine.CreatedAt.Should().BeOnOrAfter(before);
        tontine.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_InitiallyHasNoRounds()
    {
        var tontine = CreateDraftTontine();

        tontine.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void Create_StartedAtIsNull()
    {
        var tontine = CreateDraftTontine();

        tontine.StartedAt.Should().BeNull();
    }

    // ── 6-9: AddMember edge cases ───────────────────────────────────

    [Fact]
    public void AddMember_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.AddMember("NewMember");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void AddMember_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.AddMember("NewMember");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void AddMember_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateCancelledTontine();

        var act = () => tontine.AddMember("NewMember");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void AddMember_MultipleMembersGetIncrementalRangs()
    {
        var tontine = CreateDraftTontine(maxMembers: 5);

        var m1 = tontine.AddMember("Alice");
        var m2 = tontine.AddMember("Bob");
        var m3 = tontine.AddMember("Charlie");

        m1.Rang.Should().Be(1);
        m2.Rang.Should().Be(2);
        m3.Rang.Should().Be(3);
    }

    // ── 10-13: RemoveMember edge cases ──────────────────────────────

    [Fact]
    public void RemoveMember_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();
        var memberId = tontine.Members.First().Id;

        var act = () => tontine.RemoveMember(memberId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void RemoveMember_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();
        var memberId = tontine.Members.First().Id;

        var act = () => tontine.RemoveMember(memberId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void RemoveMember_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Create a draft tontine with a member, then cancel it
        var tontine = CreateDraftTontine();
        var member = tontine.AddMember("Alice");
        tontine.Cancel();

        var act = () => tontine.RemoveMember(member.Id);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void RemoveMember_RaisesMemberRemovedEvent()
    {
        var tontine = CreateDraftTontine();
        var member = tontine.AddMember("Alice");
        tontine.ClearDomainEvents();

        tontine.RemoveMember(member.Id);

        tontine.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MemberRemovedEvent>();
    }

    // ── 14-16: Start edge cases ─────────────────────────────────────

    [Fact]
    public void Start_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.Start();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Start_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.Start();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Start_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateCancelledTontine();

        var act = () => tontine.Start();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    // ── 17-22: Activate edge cases ──────────────────────────────────

    [Fact]
    public void Activate_WhenActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();

        var act = () => tontine.Activate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Activate_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.Activate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Activate_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.Activate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Activate_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateCancelledTontine();

        var act = () => tontine.Activate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Activate_OpensFirstRound_WithRoundNumber1()
    {
        var tontine = CreateDraftTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");

        tontine.Activate();

        tontine.Rounds.Should().ContainSingle();
        tontine.Rounds.First().RoundNumber.Should().Be(1);
        tontine.Rounds.First().IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Activate_SequentialMode_FirstBeneficiaryIsLowestRang()
    {
        var tontine = CreateDraftTontine(mode: ModeAttribution.Sequentiel);
        var m1 = tontine.AddMember("Alice");   // Rang 1
        tontine.AddMember("Bob");               // Rang 2
        tontine.AddMember("Charlie");           // Rang 3

        tontine.Activate();

        var firstRound = tontine.Rounds.First();
        firstRound.BeneficiaryId.Should().Be(m1.Id);
    }

    // ── 23-24: Cancel edge cases ────────────────────────────────────

    [Fact]
    public void Cancel_WhenSuspended_CancelsTontine()
    {
        var tontine = CreateSuspendedTontine();

        tontine.Cancel();

        tontine.Status.Should().Be(TontineStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.Cancel();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*cannot*cancelled*");
    }

    // ── 25: Suspendre edge case ─────────────────────────────────────

    [Fact]
    public void Suspendre_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.Suspendre();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    // ── 26-28: OuvrirTour edge cases ────────────────────────────────

    [Fact]
    public void OuvrirTour_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.OuvrirTour();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public void OuvrirTour_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.OuvrirTour();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public void OuvrirTour_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateCancelledTontine();

        var act = () => tontine.OuvrirTour();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    // ── 29-30: CloseRound edge cases ────────────────────────────────

    [Fact]
    public void CloseRound_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();
        var roundId = tontine.Rounds.First().Id;
        tontine.Suspendre();

        var act = () => tontine.CloseRound(roundId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public void CloseRound_AutoOpensNextRound_WithCorrectRoundNumber()
    {
        var tontine = CreateActiveTontine(memberCount: 3, maxMembers: 5);
        var firstRound = tontine.Rounds.First();
        tontine.ClearDomainEvents();

        tontine.CloseRound(firstRound.Id);

        tontine.Rounds.Should().HaveCount(2);
        var secondRound = tontine.Rounds.Last();
        secondRound.RoundNumber.Should().Be(2);
        secondRound.IsCompleted.Should().BeFalse();
        firstRound.IsCompleted.Should().BeTrue();
    }

    // ── 31-32: GetActiveRounds / GetActiveMembers ───────────────────

    [Fact]
    public void GetActiveRounds_ReturnsOnlyNonCompletedRounds()
    {
        var tontine = CreateActiveTontine(memberCount: 3, maxMembers: 5);
        var firstRound = tontine.Rounds.First();
        tontine.CloseRound(firstRound.Id);

        var activeRounds = tontine.GetActiveRounds();

        activeRounds.Should().ContainSingle();
        activeRounds.Should().OnlyContain(r => !r.IsCompleted);
    }

    [Fact]
    public void GetActiveMembers_ReturnsOnlyActifMembers()
    {
        var tontine = CreateActiveTontine(memberCount: 4, maxMembers: 5);
        var memberToSuspend = tontine.Members.Last();
        tontine.SuspendMember(memberToSuspend.Id);

        var activeMembers = tontine.GetActiveMembers();

        activeMembers.Should().HaveCount(3);
        activeMembers.Should().NotContain(m => m.Id == memberToSuspend.Id);
        activeMembers.Should().OnlyContain(m => m.Statut == StatutMembre.Actif);
    }

    // ── 33-34: SuspendMember edge cases ─────────────────────────────

    [Fact]
    public void SuspendMember_NonExistentMember_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();
        var fakeMemberId = MemberId.Create();

        var act = () => tontine.SuspendMember(fakeMemberId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void SuspendMember_WhenDraft_ThrowsInvalidOperationException()
    {
        var tontine = CreateDraftTontine();
        var member = tontine.AddMember("Alice");

        var act = () => tontine.SuspendMember(member.Id);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    // ── 35-36: JoinWithInvitation edge cases ────────────────────────

    [Fact]
    public void JoinWithInvitation_WhenFull_ThrowsInvalidOperationException()
    {
        var tontine = CreateDraftTontine(maxMembers: 3);
        var (_, plainCode) = tontine.GenerateInvitation(nombreUsagesMax: 5);
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");

        var userId = UtilisateurId.Create();
        var act = () => tontine.JoinWithInvitation("Dave", plainCode, userId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add more than*");
    }

    [Fact]
    public void JoinWithInvitation_DuplicateName_ThrowsInvalidOperationException()
    {
        var tontine = CreateDraftTontine(maxMembers: 5);
        var (_, plainCode) = tontine.GenerateInvitation(nombreUsagesMax: 5);
        tontine.JoinWithInvitation("Alice", plainCode, UtilisateurId.Create());

        var act = () => tontine.JoinWithInvitation("Alice", plainCode, UtilisateurId.Create());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*name*already exists*");
    }

    // ── 37-39: GenerateInvitation edge cases ────────────────────────

    [Fact]
    public void GenerateInvitation_WhenSuspended_ThrowsInvalidOperationException()
    {
        var tontine = CreateSuspendedTontine();

        var act = () => tontine.GenerateInvitation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void GenerateInvitation_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateCompletedTontine();

        var act = () => tontine.GenerateInvitation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void GenerateInvitation_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateCancelledTontine();

        var act = () => tontine.GenerateInvitation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    // ── 40: PopDomainEvents ─────────────────────────────────────────

    [Fact]
    public void PopDomainEvents_ClearsAndReturnsEvents()
    {
        var tontine = CreateDraftTontine();
        tontine.AddMember("Alice");

        // Should have TontineCreatedEvent + MemberAddedEvent
        tontine.DomainEvents.Should().HaveCountGreaterThanOrEqualTo(2);

        var popped = tontine.PopDomainEvents();

        popped.Should().HaveCountGreaterThanOrEqualTo(2);
        popped.Should().Contain(e => e is TontineCreatedEvent);
        popped.Should().Contain(e => e is MemberAddedEvent);
        tontine.DomainEvents.Should().BeEmpty();
    }
}
