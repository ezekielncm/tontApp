using Domain.IdentityManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;

namespace DomainUnitsTest;

public class TontineAggregateTests
{
    private static readonly UtilisateurId TestGestionnaireId = UtilisateurId.Create();

    private static Tontine CreateDefaultTontine(
        string name = "Tontine Test",
        decimal amount = 5000m,
        string currency = "XOF",
        int maxMembers = 5,
        ModeAttribution modeAttribution = ModeAttribution.Sequentiel)
    {
        var contribution = ContributionAmount.Create(amount, currency);
        return Tontine.Create(name, "Description test", contribution, TontinePeriodicity.Monthly, maxMembers, TestGestionnaireId, modeAttribution);
    }

    private static Tontine CreateActiveTontine(int memberCount = 3)
    {
        var tontine = CreateDefaultTontine(maxMembers: Math.Max(memberCount, 5));
        for (int i = 1; i <= memberCount; i++)
            tontine.AddMember($"Member{i}");
        tontine.Activate();
        return tontine;
    }

    // ── Reglement immutability ──────────────────────────────────────

    [Fact]
    public void Create_SetsReglementWithCorrectValues()
    {
        var contribution = ContributionAmount.Create(5000m, "XOF");
        var tontine = Tontine.Create("Tontine A", null, contribution, TontinePeriodicity.Weekly, 10, TestGestionnaireId, ModeAttribution.Aleatoire);

        Assert.Equal(5000m, tontine.Reglement.ContributionAmount.Amount);
        Assert.Equal("XOF", tontine.Reglement.ContributionAmount.Currency);
        Assert.Equal(TontinePeriodicity.Weekly, tontine.Reglement.Periodicity);
        Assert.Equal(10, tontine.Reglement.MaxMembers);
        Assert.Equal(ModeAttribution.Aleatoire, tontine.Reglement.ModeAttribution);
        Assert.Equal(3, tontine.Reglement.MinMembresActivation);
    }

    [Fact]
    public void Reglement_ComputedPropertiesMatchReglementValues()
    {
        var tontine = CreateDefaultTontine();

        Assert.Equal(tontine.Reglement.ContributionAmount, tontine.ContributionAmount);
        Assert.Equal(tontine.Reglement.Periodicity, tontine.Periodicity);
        Assert.Equal(tontine.Reglement.MaxMembers, tontine.MaxMembers);
        Assert.Equal(tontine.Reglement.ModeAttribution, tontine.ModeAttribution);
    }

    [Fact]
    public void Reglement_Create_WithInvalidMaxMembers_ThrowsArgumentException()
    {
        var contribution = ContributionAmount.Create(100m, "XOF");
        Assert.Throws<ArgumentException>(() =>
            Reglement.Create(contribution, TontinePeriodicity.Monthly, 1));
    }

    [Fact]
    public void Reglement_Create_WithMinMembresExceedingMax_ThrowsArgumentException()
    {
        var contribution = ContributionAmount.Create(100m, "XOF");
        Assert.Throws<ArgumentException>(() =>
            Reglement.Create(contribution, TontinePeriodicity.Monthly, 5, minMembresActivation: 10));
    }

    [Fact]
    public void Reglement_Create_WithNullContribution_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Reglement.Create(null!, TontinePeriodicity.Monthly, 5));
    }

    [Fact]
    public void Reglement_Equality_SameValues_AreEqual()
    {
        var contribution1 = ContributionAmount.Create(5000m, "XOF");
        var contribution2 = ContributionAmount.Create(5000m, "XOF");
        var r1 = Reglement.Create(contribution1, TontinePeriodicity.Monthly, 5);
        var r2 = Reglement.Create(contribution2, TontinePeriodicity.Monthly, 5);

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void Reglement_Equality_DifferentValues_AreNotEqual()
    {
        var contribution = ContributionAmount.Create(5000m, "XOF");
        var r1 = Reglement.Create(contribution, TontinePeriodicity.Monthly, 5);
        var r2 = Reglement.Create(contribution, TontinePeriodicity.Weekly, 5);

        Assert.NotEqual(r1, r2);
    }

    // ── Minimum 3 members for activation ────────────────────────────

    [Fact]
    public void Activate_WithExactlyThreeMembers_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("A");
        tontine.AddMember("B");
        tontine.AddMember("C");

        tontine.Activate();

        Assert.Equal(TontineStatus.Active, tontine.Status);
    }

    [Fact]
    public void Activate_WithTwoMembers_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("A");
        tontine.AddMember("B");

        var ex = Assert.Throws<InvalidOperationException>(() => tontine.Activate());
        Assert.Contains("at least 3 members", ex.Message);
    }

    [Fact]
    public void Activate_WithOneMember_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("A");

        Assert.Throws<InvalidOperationException>(() => tontine.Activate());
    }

    [Fact]
    public void Activate_WithZeroMembers_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.Activate());
    }

    // ── OuvrirTour ──────────────────────────────────────────────────

    [Fact]
    public void OuvrirTour_WhenNotActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.OuvrirTour());
    }

    [Fact]
    public void OuvrirTour_WhenRoundAlreadyOpen_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();
        // Activate already opens the first round, so trying to open another should fail
        Assert.Throws<InvalidOperationException>(() => tontine.OuvrirTour());
    }

    [Fact]
    public void OuvrirTour_AfterClosingPreviousRound_Succeeds()
    {
        var tontine = CreateActiveTontine(4);

        // Close the first round (opened by Activate)
        var firstRound = tontine.Rounds.First();
        tontine.CloseRound(firstRound.Id);
        // CloseRound automatically opens the next round, close that too
        var secondRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Skip(1).First();
        tontine.CloseRound(secondRound.Id);
        // Now auto-opened third round - close it
        var thirdRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Skip(2).First();
        tontine.CloseRound(thirdRound.Id);

        // All remaining members should have been served, should auto-open 4th
        // Actually with 4 members, 3 served, 1 remaining - auto-opened
        var fourthRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Last();
        Assert.False(fourthRound.IsCompleted);
    }

    [Fact]
    public void OuvrirTour_RaisesRoundOpenedEvent()
    {
        var tontine = CreateActiveTontine(4);
        tontine.ClearDomainEvents();

        // Close first round to get a new one auto-opened
        var round = tontine.Rounds.First();
        tontine.CloseRound(round.Id);

        Assert.Contains(tontine.DomainEvents, e => e is RoundOpenedEvent);
    }

    // ── CloturerTour (CloseRound) ───────────────────────────────────

    [Fact]
    public void CloseRound_WhenNotActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("A");
        tontine.AddMember("B");
        tontine.AddMember("C");

        Assert.Throws<InvalidOperationException>(() => tontine.CloseRound(RoundId.Create()));
    }

    [Fact]
    public void CloseRound_WithNonExistentRound_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.CloseRound(RoundId.Create()));
    }

    [Fact]
    public void CloseRound_AlreadyClosed_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine();
        var round = tontine.Rounds.First();
        tontine.CloseRound(round.Id);

        Assert.Throws<InvalidOperationException>(() => tontine.CloseRound(round.Id));
    }

    [Fact]
    public void CloseRound_RaisesRoundClosedEvent()
    {
        var tontine = CreateActiveTontine();
        var round = tontine.Rounds.First();
        tontine.ClearDomainEvents();

        tontine.CloseRound(round.Id);

        Assert.Contains(tontine.DomainEvents, e => e is RoundClosedEvent);
    }

    [Fact]
    public void CloseRound_WithRemainingMembers_OpensNextRound()
    {
        var tontine = CreateActiveTontine(4);
        var round = tontine.Rounds.First();

        tontine.CloseRound(round.Id);

        Assert.Equal(2, tontine.Rounds.Count);
    }

    [Fact]
    public void CloseRound_AllMembersServed_CompletesTontine()
    {
        var tontine = CreateActiveTontine(3);

        // Close all 3 rounds
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var openRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Last();
            tontine.CloseRound(openRound.Id);
        }

        Assert.Equal(TontineStatus.Completed, tontine.Status);
    }

    // ── Suspendre ───────────────────────────────────────────────────

    [Fact]
    public void Suspendre_WhenActive_SetsStatusToSuspended()
    {
        var tontine = CreateActiveTontine();
        tontine.ClearDomainEvents();

        tontine.Suspendre();

        Assert.Equal(TontineStatus.Suspended, tontine.Status);
    }

    [Fact]
    public void Suspendre_WhenActive_RaisesTontineSuspendedEvent()
    {
        var tontine = CreateActiveTontine();
        tontine.ClearDomainEvents();

        tontine.Suspendre();

        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<TontineSuspendedEvent>(domainEvent);
    }

    [Fact]
    public void Suspendre_WhenDraft_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.Suspendre());
    }

    [Fact]
    public void Suspendre_WhenCompleted_ThrowsInvalidOperationException()
    {
        var tontine = CreateActiveTontine(3);
        // Close all rounds to complete the tontine
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var openRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Last();
            tontine.CloseRound(openRound.Id);
        }

        Assert.Throws<InvalidOperationException>(() => tontine.Suspendre());
    }

    [Fact]
    public void Suspendre_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.Cancel();

        Assert.Throws<InvalidOperationException>(() => tontine.Suspendre());
    }

    // ── Clore ───────────────────────────────────────────────────────

    [Fact]
    public void Clore_WhenActive_SetsStatusToCompleted()
    {
        var tontine = CreateActiveTontine();

        tontine.Clore();

        Assert.Equal(TontineStatus.Completed, tontine.Status);
    }

    [Fact]
    public void Clore_WhenSuspended_SetsStatusToCompleted()
    {
        var tontine = CreateActiveTontine();
        tontine.Suspendre();

        tontine.Clore();

        Assert.Equal(TontineStatus.Completed, tontine.Status);
    }

    [Fact]
    public void Clore_WhenDraft_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.Clore());
    }

    [Fact]
    public void Clore_WhenCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.Cancel();

        Assert.Throws<InvalidOperationException>(() => tontine.Clore());
    }

    // ── Encapsulation ───────────────────────────────────────────────

    [Fact]
    public void Members_ReturnsReadOnlyCollection()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");

        var members = tontine.Members;

        Assert.IsAssignableFrom<IReadOnlyCollection<Member>>(members);
    }

    [Fact]
    public void Rounds_ReturnsReadOnlyCollection()
    {
        var tontine = CreateActiveTontine();

        var rounds = tontine.Rounds;

        Assert.IsAssignableFrom<IReadOnlyCollection<Round>>(rounds);
    }

    // ── Domain events ───────────────────────────────────────────────

    [Fact]
    public void TontineCreatedEvent_HasCorrectProperties()
    {
        var tontine = CreateDefaultTontine(name: "Ma Tontine");

        var evt = tontine.DomainEvents.OfType<TontineCreatedEvent>().Single();

        Assert.Equal(tontine.Id, evt.TontineId);
        Assert.Equal("Ma Tontine", evt.Name);
        Assert.True(evt.OccurredOn <= DateTime.UtcNow);
    }

    [Fact]
    public void MemberAddedEvent_HasCorrectProperties()
    {
        var tontine = CreateDefaultTontine();
        tontine.ClearDomainEvents();

        tontine.AddMember("Alice");

        var evt = tontine.DomainEvents.OfType<MemberAddedEvent>().Single();
        Assert.Equal(tontine.Id, evt.TontineId);
        Assert.Equal("Alice", evt.MemberName);
    }

    [Fact]
    public void RoundOpenedEvent_HasCorrectProperties()
    {
        var tontine = CreateActiveTontine();

        var evt = tontine.DomainEvents.OfType<RoundOpenedEvent>().Single();
        Assert.Equal(tontine.Id, evt.TontineId);
        Assert.Equal(1, evt.RoundNumber);
    }

    [Fact]
    public void RoundClosedEvent_HasCorrectProperties()
    {
        var tontine = CreateActiveTontine();
        var round = tontine.Rounds.First();
        tontine.ClearDomainEvents();

        tontine.CloseRound(round.Id);

        var evt = tontine.DomainEvents.OfType<RoundClosedEvent>().Single();
        Assert.Equal(tontine.Id, evt.TontineId);
        Assert.Equal(round.Id, evt.RoundId);
        Assert.Equal(1, evt.RoundNumber);
    }

    [Fact]
    public void MemberSuspendedEvent_HasCorrectProperties()
    {
        var tontine = CreateActiveTontine();
        var member = tontine.Members.First();
        tontine.ClearDomainEvents();

        tontine.SuspendMember(member.Id);

        var evt = tontine.DomainEvents.OfType<MemberSuspendedEvent>().Single();
        Assert.Equal(tontine.Id, evt.TontineId);
        Assert.Equal(member.Id, evt.MemberId);
    }

    // ── Full lifecycle ──────────────────────────────────────────────

    [Fact]
    public void FullLifecycle_Create_AddMembers_Activate_CloseAllRounds_Completes()
    {
        var tontine = CreateDefaultTontine(maxMembers: 3);
        Assert.Equal(TontineStatus.Draft, tontine.Status);

        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
        Assert.Equal(3, tontine.Members.Count);

        tontine.Activate();
        Assert.Equal(TontineStatus.Active, tontine.Status);
        Assert.Single(tontine.Rounds);

        // Close all 3 rounds
        for (int i = 0; i < 3; i++)
        {
            var round = tontine.Rounds.OrderBy(r => r.RoundNumber).Last();
            tontine.CloseRound(round.Id);
        }

        Assert.Equal(TontineStatus.Completed, tontine.Status);
        Assert.Equal(3, tontine.Rounds.Count);
        Assert.All(tontine.Rounds, r => Assert.True(r.IsCompleted));
    }

    [Fact]
    public void FullLifecycle_Create_Activate_Suspend_Close()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("A");
        tontine.AddMember("B");
        tontine.AddMember("C");

        tontine.Activate();
        Assert.Equal(TontineStatus.Active, tontine.Status);

        tontine.Suspendre();
        Assert.Equal(TontineStatus.Suspended, tontine.Status);

        tontine.Clore();
        Assert.Equal(TontineStatus.Completed, tontine.Status);
    }
}
