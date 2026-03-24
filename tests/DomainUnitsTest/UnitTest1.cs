using Domain.TontineManagement;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.ValueObjects;

namespace DomainUnitsTest;

public class TontineTests
{
    private static Tontine CreateDefaultTontine(
        string name = "Test Tontine",
        decimal amount = 100m,
        string currency = "XOF",
        int maxMembers = 5)
    {
        var contribution = ContributionAmount.Create(amount, currency);
        return Tontine.Create(name, "A test tontine", contribution, TontinePeriodicity.Monthly, maxMembers);
    }

    [Fact]
    public void Create_WithValidParameters_ReturnsTontineInDraftStatus()
    {
        var tontine = CreateDefaultTontine();

        Assert.Equal("Test Tontine", tontine.Name);
        Assert.Equal(TontineStatus.Draft, tontine.Status);
        Assert.Equal(5, tontine.MaxMembers);
        Assert.Empty(tontine.Members);
    }

    [Fact]
    public void Create_WithValidParameters_RaisesTontineCreatedEvent()
    {
        var tontine = CreateDefaultTontine();

        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<TontineCreatedEvent>(domainEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsArgumentException(string? name)
    {
        var contribution = ContributionAmount.Create(100, "XOF");

        Assert.Throws<ArgumentException>(() =>
            Tontine.Create(name!, null, contribution, TontinePeriodicity.Monthly, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void Create_WithInvalidMaxMembers_ThrowsArgumentException(int maxMembers)
    {
        var contribution = ContributionAmount.Create(100, "XOF");

        Assert.Throws<ArgumentException>(() =>
            Tontine.Create("Test", null, contribution, TontinePeriodicity.Monthly, maxMembers));
    }

    [Fact]
    public void AddMember_WhenDraft_AddsMemberSuccessfully()
    {
        var tontine = CreateDefaultTontine();

        var member = tontine.AddMember("Alice");

        Assert.Single(tontine.Members);
        Assert.Equal("Alice", member.Name);
    }

    [Fact]
    public void AddMember_WhenDraft_RaisesMemberAddedEvent()
    {
        var tontine = CreateDefaultTontine();
        tontine.ClearDomainEvents();

        tontine.AddMember("Alice");

        var domainEvent = Assert.Single(tontine.DomainEvents);
        var memberAddedEvent = Assert.IsType<MemberAddedEvent>(domainEvent);
        Assert.Equal("Alice", memberAddedEvent.MemberName);
    }

    [Fact]
    public void AddMember_WhenMaxReached_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine(maxMembers: 2);
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");

        Assert.Throws<InvalidOperationException>(() => tontine.AddMember("Charlie"));
    }

    [Fact]
    public void AddMember_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");

        Assert.Throws<InvalidOperationException>(() => tontine.AddMember("Alice"));
    }

    [Fact]
    public void AddMember_WhenActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();

        Assert.Throws<InvalidOperationException>(() => tontine.AddMember("Charlie"));
    }

    [Fact]
    public void RemoveMember_WhenDraft_RemovesMemberSuccessfully()
    {
        var tontine = CreateDefaultTontine();
        var member = tontine.AddMember("Alice");
        tontine.ClearDomainEvents();

        tontine.RemoveMember(member.Id);

        Assert.Empty(tontine.Members);
        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<MemberRemovedEvent>(domainEvent);
    }

    [Fact]
    public void RemoveMember_WhenActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        var member = tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();

        Assert.Throws<InvalidOperationException>(() => tontine.RemoveMember(member.Id));
    }

    [Fact]
    public void RemoveMember_NonExistentMember_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();

        Assert.Throws<InvalidOperationException>(() => tontine.RemoveMember(MemberId.Create()));
    }

    [Fact]
    public void Start_WithEnoughMembers_ActivatesTontine()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.ClearDomainEvents();

        tontine.Start();

        Assert.Equal(TontineStatus.Active, tontine.Status);
        Assert.NotNull(tontine.StartedAt);
        var domainEvent = Assert.Single(tontine.DomainEvents);
        Assert.IsType<TontineStartedEvent>(domainEvent);
    }

    [Fact]
    public void Start_WithLessThanTwoMembers_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");

        Assert.Throws<InvalidOperationException>(() => tontine.Start());
    }

    [Fact]
    public void Start_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();

        Assert.Throws<InvalidOperationException>(() => tontine.Start());
    }

    [Fact]
    public void Cancel_WhenDraft_CancelsTontine()
    {
        var tontine = CreateDefaultTontine();

        tontine.Cancel();

        Assert.Equal(TontineStatus.Cancelled, tontine.Status);
    }

    [Fact]
    public void Cancel_WhenActive_CancelsTontine()
    {
        var tontine = CreateDefaultTontine();
        tontine.AddMember("Alice");
        tontine.AddMember("Bob");
        tontine.Start();

        tontine.Cancel();

        Assert.Equal(TontineStatus.Cancelled, tontine.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var tontine = CreateDefaultTontine();
        tontine.Cancel();

        Assert.Throws<InvalidOperationException>(() => tontine.Cancel());
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var tontine = CreateDefaultTontine();
        Assert.NotEmpty(tontine.DomainEvents);

        tontine.ClearDomainEvents();

        Assert.Empty(tontine.DomainEvents);
    }
}

public class ContributionAmountTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsContributionAmount()
    {
        var amount = ContributionAmount.Create(1000m, "XOF");

        Assert.Equal(1000m, amount.Amount);
        Assert.Equal("XOF", amount.Currency);
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperCase()
    {
        var amount = ContributionAmount.Create(100m, "xof");

        Assert.Equal("XOF", amount.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidAmount_ThrowsArgumentException(decimal value)
    {
        Assert.Throws<ArgumentException>(() => ContributionAmount.Create(value, "XOF"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidCurrency_ThrowsArgumentException(string? currency)
    {
        Assert.Throws<ArgumentException>(() => ContributionAmount.Create(100m, currency!));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = ContributionAmount.Create(100m, "XOF");
        var b = ContributionAmount.Create(100m, "XOF");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = ContributionAmount.Create(100m, "XOF");
        var b = ContributionAmount.Create(200m, "XOF");

        Assert.NotEqual(a, b);
    }
}

public class TontineIdTests
{
    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var id1 = TontineId.Create();
        var id2 = TontineId.Create();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TontineId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithValidGuid_ReturnsId()
    {
        var guid = Guid.NewGuid();
        var id = TontineId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = TontineId.From(guid);
        var id2 = TontineId.From(guid);

        Assert.Equal(id1, id2);
    }
}

public class MemberTests
{
    [Fact]
    public void Create_WithValidName_ReturnsMember()
    {
        var member = Domain.TontineManagement.Entities.Member.Create("Alice");

        Assert.Equal("Alice", member.Name);
        Assert.NotEqual(default, member.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            Domain.TontineManagement.Entities.Member.Create(name!));
    }
}
