using Domain.TontineManagement;
using Domain.TontineManagement.Entities;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;

namespace DomainUnitsTest;

public class TontineEnhancedTests
{
    private static Tontine CreateDefaultTontine(
        string name = "Test Tontine",
        decimal amount = 100m,
        string currency = "XOF",
        int maxMembers = 5,
        ModeAttribution modeAttribution = ModeAttribution.Sequentiel)
    {
        var contribution = ContributionAmount.Create(amount, currency);
        return Tontine.Create(name, "A test tontine", contribution, TontinePeriodicity.Monthly, maxMembers, modeAttribution);
    }

    [Fact]
    public void Create_WithModeAttribution_SetsModeAttribution()
    {
        var tontine = CreateDefaultTontine(modeAttribution: ModeAttribution.Aleatoire);

        Assert.Equal(ModeAttribution.Aleatoire, tontine.ModeAttribution);
        Assert.Equal(TontineStatus.Draft, tontine.Status);
    }

    [Fact]
    public void Create_DefaultModeAttribution_IsSequentiel()
    {
        var tontine = CreateDefaultTontine();

        Assert.Equal(ModeAttribution.Sequentiel, tontine.ModeAttribution);
    }

    [Fact]
    public void GenerateInvitation_WhenDraft_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.ClearDomainEvents();

        var invitation = tontine.GenerateInvitation();

        Assert.Single(tontine.Invitations);
        Assert.NotNull(invitation.Code);
        Assert.False(invitation.IsUsed);
        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<InvitationGeneratedEvent>(domainEvent);
    }

    [Fact]
    public void GenerateInvitation_WhenActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();

        Assert.Throws<InvalidOperationException>(() => tontine.GenerateInvitation());
    }

    [Fact]
    public void JoinWithInvitation_WithValidCode_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        var invitation = tontine.GenerateInvitation();
        tontine.ClearDomainEvents();

        var member = tontine.JoinWithInvitation("Bob", invitation.Code.ToString());

        Assert.Equal("Bob", member.Name);
        Assert.Equal(2, tontine.Members.Count);
    }

    [Fact]
    public void JoinWithInvitation_MemberGetsCorrectRank()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        var invitation = tontine.GenerateInvitation();

        var member = tontine.JoinWithInvitation("Bob", invitation.Code.ToString());

        Assert.Equal(2, member.Rang);
    }

    [Fact]
    public void JoinWithInvitation_WithInvalidCode_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.GenerateInvitation();

        Assert.Throws<InvalidOperationException>(() =>
            tontine.JoinWithInvitation("Bob", "ZZZZZZ"));
    }

    [Fact]
    public void JoinWithInvitation_WithUsedSingleUseCode_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        var invitation = tontine.GenerateInvitation(isMultipleUse: false);
        tontine.JoinWithInvitation("Alice", invitation.Code.ToString());

        Assert.Throws<InvalidOperationException>(() =>
            tontine.JoinWithInvitation("Bob", invitation.Code.ToString()));
    }

    [Fact]
    public void Activate_WithEnoughMembers_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.ClearDomainEvents();

        tontine.Activate();

        Assert.Equal(TontineStatus.Active, tontine.Status);
        Assert.NotNull(tontine.StartedAt);
        Assert.Single(tontine.Rounds);
    }

    [Fact]
    public void Activate_RaisesActivatedAndRoundOpenedEvents()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.ClearDomainEvents();

        tontine.Activate();

        Assert.Equal(2, tontine.DomainEvents.Count);
        Assert.Contains(tontine.DomainEvents, e => e is TontineActivatedEvent);
        Assert.Contains(tontine.DomainEvents, e => e is RoundOpenedEvent);
    }

    [Fact]
    public void Activate_WithLessThanTwoMembers_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");

        Assert.Throws<InvalidOperationException>(() => tontine.Activate());
    }

    [Fact]
    public void SuspendMember_WhenActive_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        var alice = tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();
        tontine.ClearDomainEvents();

        tontine.SuspendMember(alice.Id);

        Assert.Equal(StatutMembre.Suspendu, alice.Statut);
        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<MemberSuspendedEvent>(domainEvent);
    }

    [Fact]
    public void SuspendMember_WhenNotActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        var alice = tontine.AddMember("Alice");

        Assert.Throws<InvalidOperationException>(() => tontine.SuspendMember(alice.Id));
    }

    [Fact]
    public void CloseRound_MarksRoundCompleted_CreatesNextRound()
    {
        var tontine = CreateDefaultTontine(maxMembers: 3);
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
        tontine.Activate();

        var firstRound = tontine.Rounds.First();
        tontine.ClearDomainEvents();

        tontine.CloseRound(firstRound.Id);

        Assert.True(firstRound.IsCompleted);
        Assert.Equal(2, tontine.Rounds.Count);
        Assert.Contains(tontine.DomainEvents, e => e is RoundClosedEvent);
        Assert.Contains(tontine.DomainEvents, e => e is RoundOpenedEvent);
    }

    [Fact]
    public void CloseRound_OnLastMember_CompletesTontine()
    {
        var tontine = CreateDefaultTontine(maxMembers: 2);
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Activate();

        // Close first round
        var firstRound = tontine.Rounds.First();
        tontine.CloseRound(firstRound.Id);

        // Close second round (last member)
        var secondRound = tontine.Rounds.Last();
        tontine.CloseRound(secondRound.Id);

        Assert.Equal(TontineStatus.Completed, tontine.Status);
    }
}

public class MemberEnhancedTests
{
    [Fact]
    public void Suspendre_SetsStatutToSuspendu()
    {
        var member = Member.Create("Alice");

        member.Suspendre();

        Assert.Equal(StatutMembre.Suspendu, member.Statut);
    }

    [Fact]
    public void Reactiver_SetsStatutToActif()
    {
        var member = Member.Create("Alice");
        member.Suspendre();

        member.Reactiver();

        Assert.Equal(StatutMembre.Actif, member.Statut);
    }

    [Fact]
    public void Create_WithRang_SetsRang()
    {
        var member = Member.Create("Alice", 3);

        Assert.Equal(3, member.Rang);
    }
}

public class InvitationCodeTests
{
    [Fact]
    public void Generate_Creates6CharAlphanumericCode()
    {
        var code = InvitationCode.Generate();

        Assert.Equal(6, code.Value.Length);
        Assert.True(code.Value.All(c => char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void From_WithValidCode_Succeeds()
    {
        var code = InvitationCode.From("ABC123");

        Assert.Equal("ABC123", code.Value);
    }

    [Fact]
    public void From_WithLowerCaseCode_NormalizesToUpperCase()
    {
        var code = InvitationCode.From("abc123");

        Assert.Equal("ABC123", code.Value);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGH")]
    public void From_WithWrongLength_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => InvitationCode.From(value));
    }

    [Theory]
    [InlineData("ABC-12")]
    [InlineData("AB C12")]
    public void From_WithNonAlphanumericChars_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => InvitationCode.From(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void From_WithEmptyOrNull_ThrowsArgumentException(string? value)
    {
        Assert.Throws<ArgumentException>(() => InvitationCode.From(value!));
    }
}

public class InvitationTests
{
    [Fact]
    public void IsValid_BeforeExpiry_ReturnsTrue()
    {
        var invitation = Invitation.Create();

        Assert.True(invitation.IsValid());
    }

    [Fact]
    public void IsValid_AfterMarkUsed_SingleUse_ReturnsFalse()
    {
        var invitation = Invitation.Create(isMultipleUse: false);

        invitation.MarkUsed();

        Assert.False(invitation.IsValid());
    }

    [Fact]
    public void MultiUse_StaysValidAfterMarkUsed()
    {
        var invitation = Invitation.Create(isMultipleUse: true);

        invitation.MarkUsed();

        Assert.True(invitation.IsValid());
        Assert.False(invitation.IsUsed);
    }

    [Fact]
    public void Create_SetsExpiresAt()
    {
        var before = DateTime.UtcNow;

        var invitation = Invitation.Create();

        Assert.True(invitation.ExpiresAt > before);
    }

    [Fact]
    public void Create_SingleUse_IsNotMultipleUse()
    {
        var invitation = Invitation.Create(isMultipleUse: false);

        Assert.False(invitation.IsMultipleUse);
    }

    [Fact]
    public void Create_MultipleUse_IsMultipleUse()
    {
        var invitation = Invitation.Create(isMultipleUse: true);

        Assert.True(invitation.IsMultipleUse);
    }
}
