using Domain.IdentityManagement.ValueObjects;
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

        var (invitation, plainCode) = tontine.GenerateInvitation();

        Assert.Single(tontine.Invitations);
        Assert.NotNull(plainCode);
        Assert.Equal(6, plainCode.Length);
        Assert.Equal(0, invitation.NombreUsagesActuels);
        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<InvitationGeneratedEvent>(domainEvent);
    }

    [Fact]
    public void GenerateInvitation_WhenActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
        tontine.Start();

        Assert.Throws<InvalidOperationException>(() => tontine.GenerateInvitation());
    }

    [Fact]
    public void JoinWithInvitation_WithValidCode_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        var (_, plainCode) = tontine.GenerateInvitation();
        tontine.ClearDomainEvents();

        var userId = UtilisateurId.Create();
        var member = tontine.JoinWithInvitation("Bob", plainCode, userId);

        Assert.Equal("Bob", member.Name);
        Assert.Equal(2, tontine.Members.Count);
        Assert.Equal(userId, member.UtilisateurId);
    }

    [Fact]
    public void JoinWithInvitation_MemberGetsCorrectRank()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        var (_, plainCode) = tontine.GenerateInvitation();

        var member = tontine.JoinWithInvitation("Bob", plainCode, UtilisateurId.Create());

        Assert.Equal(2, member.Rang);
    }

    [Fact]
    public void JoinWithInvitation_WithInvalidCode_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.GenerateInvitation();

        Assert.Throws<InvalidOperationException>(() =>
            tontine.JoinWithInvitation("Bob", "ZZZZZZ", UtilisateurId.Create()));
    }

    [Fact]
    public void JoinWithInvitation_WithUsedSingleUseCode_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        var (_, plainCode) = tontine.GenerateInvitation(nombreUsagesMax: 1);
        tontine.JoinWithInvitation("Alice", plainCode, UtilisateurId.Create());

        Assert.Throws<InvalidOperationException>(() =>
            tontine.JoinWithInvitation("Bob", plainCode, UtilisateurId.Create()));
    }

    [Fact]
    public void JoinWithInvitation_SameUserCannotJoinTwice()
    {
        var tontine = CreateDefaultTontine();
        var (_, plainCode) = tontine.GenerateInvitation(nombreUsagesMax: 5);
        var userId = UtilisateurId.Create();
        tontine.JoinWithInvitation("Alice", plainCode, userId);

        Assert.Throws<InvalidOperationException>(() =>
            tontine.JoinWithInvitation("Bob", plainCode, userId));
    }

    [Fact]
    public void JoinWithInvitation_MultiUseCode_AllowsMultipleJoins()
    {
        var tontine = CreateDefaultTontine();
        var (invitation, plainCode) = tontine.GenerateInvitation(nombreUsagesMax: 3);

        tontine.JoinWithInvitation("Alice", plainCode, UtilisateurId.Create());
        tontine.JoinWithInvitation("Bob", plainCode, UtilisateurId.Create());

        Assert.Equal(2, tontine.Members.Count);
        Assert.Equal(2, invitation.NombreUsagesActuels);
        Assert.True(invitation.IsValid()); // Still valid (2 < 3)
    }

    [Fact]
    public void Activate_WithEnoughMembers_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
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
        tontine.AddMember("Charlie");
        tontine.ClearDomainEvents();

        tontine.Activate();

        Assert.Equal(2, tontine.DomainEvents.Count);
        Assert.Contains(tontine.DomainEvents, e => e is TontineActivatedEvent);
        Assert.Contains(tontine.DomainEvents, e => e is RoundOpenedEvent);
    }

    [Fact]
    public void Activate_WithLessThanThreeMembers_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");

        Assert.Throws<InvalidOperationException>(() => tontine.Activate());
    }

    [Fact]
    public void SuspendMember_WhenActive_Succeeds()
    {
        var tontine = CreateDefaultTontine();
        var alice = tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
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
        var tontine = CreateDefaultTontine(maxMembers: 3);
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.AddMember("Charlie");
        tontine.Activate();

        // Close first round
        var firstRound = tontine.Rounds.First();
        tontine.CloseRound(firstRound.Id);

        // Close second round
        var secondRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Skip(1).First();
        tontine.CloseRound(secondRound.Id);

        // Close third round (last member)
        var thirdRound = tontine.Rounds.OrderBy(r => r.RoundNumber).Last();
        tontine.CloseRound(thirdRound.Id);

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

    [Fact]
    public void Create_WithUtilisateurId_SetsUtilisateurId()
    {
        var userId = UtilisateurId.Create();
        var member = Member.Create("Alice", 1, userId);

        Assert.Equal(userId, member.UtilisateurId);
    }

    [Fact]
    public void Create_WithoutUtilisateurId_HasNullUtilisateurId()
    {
        var member = Member.Create("Alice");

        Assert.Null(member.UtilisateurId);
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
    public void Generate_UsesCryptoRandom_ProducesDifferentCodes()
    {
        // Generate multiple codes and ensure they are not all the same
        var codes = Enumerable.Range(0, 10)
            .Select(_ => InvitationCode.Generate().Value)
            .ToHashSet();

        Assert.True(codes.Count > 1, "Expected cryptographic random to produce different codes.");
    }

    [Fact]
    public void ComputeHash_ProducesConsistentHash()
    {
        var hash1 = InvitationCode.ComputeHash("ABC123");
        var hash2 = InvitationCode.ComputeHash("ABC123");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IsCaseInsensitive()
    {
        var hash1 = InvitationCode.ComputeHash("abc123");
        var hash2 = InvitationCode.ComputeHash("ABC123");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentCodes_ProduceDifferentHashes()
    {
        var hash1 = InvitationCode.ComputeHash("ABC123");
        var hash2 = InvitationCode.ComputeHash("XYZ789");

        Assert.NotEqual(hash1, hash2);
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
        var (invitation, _) = Invitation.Create();

        Assert.True(invitation.IsValid());
    }

    [Fact]
    public void IsValid_AfterIncrementUsage_SingleUse_ReturnsFalse()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 1);

        invitation.IncrementUsage();

        Assert.False(invitation.IsValid());
    }

    [Fact]
    public void MultiUse_StaysValidAfterIncrementUsage()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 3);

        invitation.IncrementUsage();

        Assert.True(invitation.IsValid());
        Assert.Equal(1, invitation.NombreUsagesActuels);
    }

    [Fact]
    public void MultiUse_ReachesMaxUsage_BecomesInvalid()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 2);

        invitation.IncrementUsage();
        invitation.IncrementUsage();

        Assert.False(invitation.IsValid());
        Assert.Equal(2, invitation.NombreUsagesActuels);
    }

    [Fact]
    public void IncrementUsage_BeyondMax_ThrowsInvalidOperationException()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 1);
        invitation.IncrementUsage();

        Assert.Throws<InvalidOperationException>(() => invitation.IncrementUsage());
    }

    [Fact]
    public void Create_SetsExpiresAt()
    {
        var before = DateTime.UtcNow;

        var (invitation, _) = Invitation.Create();

        Assert.True(invitation.ExpiresAt > before);
    }

    [Fact]
    public void Create_SetsExpirationDays()
    {
        var before = DateTime.UtcNow;

        var (invitation, _) = Invitation.Create(expirationDays: 14);

        Assert.True(invitation.ExpiresAt > before.AddDays(13));
        Assert.True(invitation.ExpiresAt <= before.AddDays(15));
    }

    [Fact]
    public void Create_SingleUse_HasNombreUsagesMaxOne()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 1);

        Assert.Equal(1, invitation.NombreUsagesMax);
        Assert.Equal(0, invitation.NombreUsagesActuels);
    }

    [Fact]
    public void Create_MultipleUse_HasCorrectNombreUsagesMax()
    {
        var (invitation, _) = Invitation.Create(nombreUsagesMax: 10);

        Assert.Equal(10, invitation.NombreUsagesMax);
        Assert.Equal(0, invitation.NombreUsagesActuels);
    }

    [Fact]
    public void Create_ReturnsPlainCode()
    {
        var (_, plainCode) = Invitation.Create();

        Assert.NotNull(plainCode);
        Assert.Equal(6, plainCode.Length);
        Assert.True(plainCode.All(c => char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void Create_StoresHashNotPlainCode()
    {
        var (invitation, plainCode) = Invitation.Create();

        // The CodeHash should NOT equal the plain code
        Assert.NotEqual(plainCode, invitation.CodeHash);
        // The CodeHash should match the computed hash
        Assert.Equal(InvitationCode.ComputeHash(plainCode), invitation.CodeHash);
    }

    [Fact]
    public void MatchesCode_WithCorrectCode_ReturnsTrue()
    {
        var (invitation, plainCode) = Invitation.Create();

        Assert.True(invitation.MatchesCode(plainCode));
    }

    [Fact]
    public void MatchesCode_WithWrongCode_ReturnsFalse()
    {
        var (invitation, _) = Invitation.Create();

        Assert.False(invitation.MatchesCode("ZZZZZZ"));
    }

    [Fact]
    public void MatchesCode_IsCaseInsensitive()
    {
        var (invitation, plainCode) = Invitation.Create();

        Assert.True(invitation.MatchesCode(plainCode.ToLowerInvariant()));
    }
}
