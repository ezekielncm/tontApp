using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Events;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

namespace DomainUnitsTest;

public class VersementTests
{
    private static Versement CreateDefaultVersement(decimal montant = 100m, string currency = "XOF")
    {
        return Versement.Create(
            TontineId.Create(),
            MemberId.Create(),
            RoundId.Create(),
            montant,
            currency);
    }

    [Fact]
    public void Create_WithValidParameters_SetsStatusEnAttente()
    {
        var versement = CreateDefaultVersement();

        Assert.Equal(VersementStatus.EnAttente, versement.Statut);
        Assert.Equal(100m, versement.Montant);
        Assert.Equal("XOF", versement.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidAmount_ThrowsArgumentException(decimal montant)
    {
        Assert.Throws<ArgumentException>(() =>
            Versement.Create(TontineId.Create(), MemberId.Create(), RoundId.Create(), montant, "XOF"));
    }

    [Fact]
    public void Create_RaisesVersementCreatedEvent()
    {
        var versement = CreateDefaultVersement();

        Assert.Contains(versement.DomainEvents, e => e is VersementCreatedEvent);
    }

    [Fact]
    public void Create_AddsInitialAuditEntry()
    {
        var versement = CreateDefaultVersement();

        Assert.Single(versement.AuditTrail);
        var entry = versement.AuditTrail.First();
        Assert.Equal("VersementCree", entry.Action);
        Assert.Equal("system", entry.ActorId);
    }

    [Fact]
    public void Confirmer_SetsStatusConfirmeAndReference()
    {
        var versement = CreateDefaultVersement();

        versement.Confirmer("REF-123");

        Assert.Equal(VersementStatus.Confirme, versement.Statut);
        Assert.Equal("REF-123", versement.ReferenceExterne);
        Assert.NotNull(versement.ConfirmedAt);
    }

    [Fact]
    public void Confirmer_RaisesVersementConfirmedEvent()
    {
        var versement = CreateDefaultVersement();
        versement.ClearDomainEvents();

        versement.Confirmer("REF-123");

        Assert.Contains(versement.DomainEvents, e => e is VersementConfirmedEvent);
    }

    [Fact]
    public void Confirmer_AddsAuditEntry()
    {
        var versement = CreateDefaultVersement();

        versement.Confirmer("REF-123");

        Assert.Equal(2, versement.AuditTrail.Count);
        var lastEntry = versement.AuditTrail.Last();
        Assert.Equal("VersementConfirme", lastEntry.Action);
    }

    [Fact]
    public void Confirmer_WhenNotEnAttente_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-123");

        Assert.Throws<InvalidOperationException>(() => versement.Confirmer("REF-456"));
    }

    [Fact]
    public void Echouer_SetsStatusEchoue()
    {
        var versement = CreateDefaultVersement();

        versement.Echouer("Payment failed");

        Assert.Equal(VersementStatus.Echoue, versement.Statut);
    }

    [Fact]
    public void Echouer_WhenNotEnAttente_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-123");

        Assert.Throws<InvalidOperationException>(() => versement.Echouer("reason"));
    }

    [Fact]
    public void VerifierIntegrite_ReturnsTrueForValidChain()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-123");

        Assert.True(versement.VerifierIntegrite());
    }

    [Fact]
    public void VerifierIntegrite_ReturnsTrueAfterMultipleEntries()
    {
        var versement = CreateDefaultVersement();

        Assert.True(versement.VerifierIntegrite());
    }
}

public class AuditEntryTests
{
    [Fact]
    public void Create_ComputesHash()
    {
        var entry = AuditEntry.Create(string.Empty, "actor1", "TestAction", "payload");

        Assert.NotNull(entry.Hash);
        Assert.NotEmpty(entry.Hash);
    }

    [Fact]
    public void VerifyIntegrity_WithCorrectPreviousHash_ReturnsTrue()
    {
        var entry = AuditEntry.Create(string.Empty, "actor1", "TestAction", "payload");

        Assert.True(entry.VerifyIntegrity(string.Empty));
    }

    [Fact]
    public void VerifyIntegrity_WithIncorrectPreviousHash_ReturnsFalse()
    {
        var entry = AuditEntry.Create(string.Empty, "actor1", "TestAction", "payload");

        Assert.False(entry.VerifyIntegrity("wronghash"));
    }
}
