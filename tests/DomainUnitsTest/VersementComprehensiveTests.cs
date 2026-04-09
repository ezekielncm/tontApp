using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Events;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using FluentAssertions;

namespace DomainUnitsTest;

/// <summary>
/// Comprehensive tests for Versement aggregate, Montant value object,
/// AuditEntry entity, and hash chain integrity.
/// Naming convention: MethodName_Scenario_ExpectedResult
/// </summary>
public class VersementComprehensiveTests
{
    private static Versement CreateDefaultVersement(decimal montant = 500m, string devise = "XOF")
    {
        return Versement.Create(
            TontineId.Create(),
            TourId.Create(),
            PayeurId.Create(),
            Montant.Create(montant, devise));
    }

    // ─── Versement Creation ────────────────────────────────────────────────

    [Fact]
    public void Create_WithMinimumAmount_Succeeds()
    {
        var versement = CreateDefaultVersement(montant: 100m);

        versement.Montant.Valeur.Should().Be(100m);
        versement.Statut.Should().Be(VersementStatus.EnAttente);
    }

    [Fact]
    public void Create_WithLargeAmount_Succeeds()
    {
        var versement = CreateDefaultVersement(montant: 1_000_000m);

        versement.Montant.Valeur.Should().Be(1_000_000m);
    }

    [Fact]
    public void Create_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;

        var versement = CreateDefaultVersement();

        versement.CreatedAt.Should().BeOnOrAfter(before);
        versement.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_ConfirmedAtIsNull()
    {
        var versement = CreateDefaultVersement();

        versement.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ReferenceExterneIsNull()
    {
        var versement = CreateDefaultVersement();

        versement.ReferenceExterne.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyHashPrecedent_UsesDefault()
    {
        var versement = Versement.Create(
            TontineId.Create(),
            TourId.Create(),
            PayeurId.Create(),
            Montant.Create(500m),
            "");

        versement.HashPrecedent.Should().BeEmpty();
        versement.HashCourant.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_WithCustomHashPrecedent_ChainsProperly()
    {
        var previousHash = "abc123def456";

        var versement = Versement.Create(
            TontineId.Create(),
            TourId.Create(),
            PayeurId.Create(),
            Montant.Create(500m),
            previousHash);

        versement.HashPrecedent.Should().Be(previousHash);
        versement.HashCourant.Should().NotBe(previousHash);
    }

    [Fact]
    public void Create_RaisesVersementCreatedEvent_WithCorrectProperties()
    {
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.Create();

        var versement = Versement.Create(
            tontineId,
            TourId.Create(),
            payeurId,
            Montant.Create(500m));

        var evt = versement.DomainEvents.OfType<VersementCreatedEvent>().Should().ContainSingle().Subject;
        evt.VersementId.Should().Be(versement.Id);
        evt.TontineId.Should().Be(tontineId);
        evt.PayeurId.Should().Be(payeurId);
        evt.Montant.Should().Be(500m);
    }

    [Fact]
    public void Create_AddsInitialAuditEntry_WithGenesisHash()
    {
        var versement = CreateDefaultVersement();

        versement.AuditTrail.Should().ContainSingle();
        var entry = versement.AuditTrail.First();
        entry.Action.Should().Be(AuditAction.VersementCree);
        entry.HashPrecedent.Should().Be(AuditEntry.GenesisHash);
    }

    // ─── Confirmer ─────────────────────────────────────────────────────────

    [Fact]
    public void Confirmer_SetsConfirmedAtToUtcNow()
    {
        var versement = CreateDefaultVersement();
        var before = DateTime.UtcNow;

        versement.Confirmer("REF-001");

        versement.ConfirmedAt.Should().NotBeNull();
        versement.ConfirmedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Confirmer_RaisesVersementConfirmedEvent_WithCorrectProperties()
    {
        var versement = CreateDefaultVersement();
        versement.ClearDomainEvents();

        versement.Confirmer("REF-001");

        var evt = versement.DomainEvents.OfType<VersementConfirmedEvent>().Should().ContainSingle().Subject;
        evt.VersementId.Should().Be(versement.Id);
        evt.ReferenceExterne.Should().Be("REF-001");
    }

    [Fact]
    public void Confirmer_AddsAuditEntry_ChainedToPrevious()
    {
        var versement = CreateDefaultVersement();
        var firstAuditHash = versement.AuditTrail.First().HashCourant;

        versement.Confirmer("REF-001");

        versement.AuditTrail.Should().HaveCount(2);
        var confirmEntry = versement.AuditTrail.Last();
        confirmEntry.Action.Should().Be(AuditAction.VersementConfirme);
        confirmEntry.HashPrecedent.Should().Be(firstAuditHash);
    }

    [Fact]
    public void Confirmer_AlreadyConfirmed_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-001");

        var act = () => versement.Confirmer("REF-002");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*pending*");
    }

    [Fact]
    public void Confirmer_AfterRejection_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Rejeter("Failed");

        var act = () => versement.Confirmer("REF-001");

        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Rejeter ───────────────────────────────────────────────────────────

    [Fact]
    public void Rejeter_SetsStatusEchoue()
    {
        var versement = CreateDefaultVersement();

        versement.Rejeter("Insufficient funds");

        versement.Statut.Should().Be(VersementStatus.Echoue);
    }

    [Fact]
    public void Rejeter_DoesNotSetConfirmedAt()
    {
        var versement = CreateDefaultVersement();

        versement.Rejeter("Failed");

        versement.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public void Rejeter_RaisesVersementRejectedEvent_WithCorrectProperties()
    {
        var versement = CreateDefaultVersement();
        versement.ClearDomainEvents();

        versement.Rejeter("Insufficient funds");

        var evt = versement.DomainEvents.OfType<VersementRejectedEvent>().Should().ContainSingle().Subject;
        evt.VersementId.Should().Be(versement.Id);
        evt.Raison.Should().Be("Insufficient funds");
    }

    [Fact]
    public void Rejeter_AddsAuditEntry_WithRejeteAction()
    {
        var versement = CreateDefaultVersement();

        versement.Rejeter("Failed");

        versement.AuditTrail.Should().HaveCount(2);
        versement.AuditTrail.Last().Action.Should().Be(AuditAction.VersementRejete);
    }

    [Fact]
    public void Rejeter_AlreadyRejected_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Rejeter("First");

        var act = () => versement.Rejeter("Second");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rejeter_AfterConfirmation_ThrowsInvalidOperationException()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-001");

        var act = () => versement.Rejeter("Too late");

        act.Should().Throw<InvalidOperationException>();
    }

    // ─── VerifierIntegrite ─────────────────────────────────────────────────

    [Fact]
    public void VerifierIntegrite_FreshVersement_ReturnsTrue()
    {
        var versement = CreateDefaultVersement();

        versement.VerifierIntegrite().Should().BeTrue();
    }

    [Fact]
    public void VerifierIntegrite_AfterConfirmation_ReturnsTrue()
    {
        var versement = CreateDefaultVersement();
        versement.Confirmer("REF-001");

        versement.VerifierIntegrite().Should().BeTrue();
    }

    [Fact]
    public void VerifierIntegrite_AfterRejection_ReturnsTrue()
    {
        var versement = CreateDefaultVersement();
        versement.Rejeter("Failed");

        versement.VerifierIntegrite().Should().BeTrue();
    }

    // ─── CalculerHash ──────────────────────────────────────────────────────

    [Fact]
    public void CalculerHash_IsDeterministic()
    {
        var id = VersementId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var montant = Montant.Create(500m, "XOF");
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = Versement.CalculerHash(id, montant, date, "prev");
        var hash2 = Versement.CalculerHash(id, montant, date, "prev");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CalculerHash_ProducesSha256Length()
    {
        var id = VersementId.Create();
        var montant = Montant.Create(500m, "XOF");

        var hash = Versement.CalculerHash(id, montant, DateTime.UtcNow, "");

        hash.Should().HaveLength(64); // SHA-256 hex string
    }

    [Fact]
    public void CalculerHash_DifferentAmounts_ProduceDifferentHashes()
    {
        var id = VersementId.Create();
        var date = DateTime.UtcNow;

        var hash1 = Versement.CalculerHash(id, Montant.Create(500m), date, "");
        var hash2 = Versement.CalculerHash(id, Montant.Create(600m), date, "");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculerHash_DifferentPreviousHash_ProduceDifferentHashes()
    {
        var id = VersementId.Create();
        var montant = Montant.Create(500m);
        var date = DateTime.UtcNow;

        var hash1 = Versement.CalculerHash(id, montant, date, "hash1");
        var hash2 = Versement.CalculerHash(id, montant, date, "hash2");

        hash1.Should().NotBe(hash2);
    }

    // ─── Hash Chain Across Versements ──────────────────────────────────────

    [Fact]
    public void HashChain_ThreeVersements_IntegrityMaintained()
    {
        var tontineId = TontineId.Create();

        var v1 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m), "");
        var v2 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(600m), v1.HashCourant);
        var v3 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(700m), v2.HashCourant);

        v1.VerifierIntegrite().Should().BeTrue();
        v2.VerifierIntegrite().Should().BeTrue();
        v3.VerifierIntegrite().Should().BeTrue();

        v2.HashPrecedent.Should().Be(v1.HashCourant);
        v3.HashPrecedent.Should().Be(v2.HashCourant);
    }

    // ─── Montant Value Object ──────────────────────────────────────────────

    [Theory]
    [InlineData(99)]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-100)]
    public void Montant_BelowMinimum_ThrowsArgumentException(decimal valeur)
    {
        var act = () => Montant.Create(valeur, "XOF");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Montant_AtExactMinimum_Succeeds()
    {
        var montant = Montant.Create(100m, "XOF");

        montant.Valeur.Should().Be(100m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Montant_EmptyDevise_ThrowsArgumentException(string? devise)
    {
        var act = () => Montant.Create(500m, devise!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Montant_Equality_SameValues_AreEqual()
    {
        var m1 = Montant.Create(500m, "XOF");
        var m2 = Montant.Create(500m, "XOF");

        m1.Should().Be(m2);
    }

    [Fact]
    public void Montant_Equality_DifferentValeur_AreNotEqual()
    {
        var m1 = Montant.Create(500m, "XOF");
        var m2 = Montant.Create(600m, "XOF");

        m1.Should().NotBe(m2);
    }

    [Fact]
    public void Montant_Equality_DifferentDevise_AreNotEqual()
    {
        var m1 = Montant.Create(500m, "XOF");
        var m2 = Montant.Create(500m, "EUR");

        m1.Should().NotBe(m2);
    }

    [Fact]
    public void Montant_ToString_FormatsCorrectly()
    {
        var montant = Montant.Create(500m, "XOF");

        montant.ToString().Should().Be("500 XOF");
    }

    // ─── AuditEntry Entity ─────────────────────────────────────────────────

    [Fact]
    public void AuditEntry_GenesisHash_Is64CharHex()
    {
        AuditEntry.GenesisHash.Should().HaveLength(64);
        AuditEntry.GenesisHash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void AuditEntry_Create_SetsAllProperties()
    {
        var tontineId = TontineId.Create();
        var versementId = VersementId.Create();

        var entry = AuditEntry.Create(tontineId, versementId, AuditAction.VersementCree, "actor1", "payload", AuditEntry.GenesisHash);

        entry.TontineId.Should().Be(tontineId);
        entry.VersementId.Should().Be(versementId);
        entry.Action.Should().Be(AuditAction.VersementCree);
        entry.ActeurId.Should().Be("actor1");
        entry.Payload.Should().Be("payload");
        entry.HashPrecedent.Should().Be(AuditEntry.GenesisHash);
        entry.HashCourant.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AuditEntry_VerifyIntegrity_CorrectChain_ReturnsTrue()
    {
        var entry = AuditEntry.Create(TontineId.Create(), VersementId.Create(), AuditAction.VersementCree, "actor", "payload", AuditEntry.GenesisHash);

        entry.VerifyIntegrity(AuditEntry.GenesisHash).Should().BeTrue();
    }

    [Fact]
    public void AuditEntry_VerifyIntegrity_WrongPreviousHash_ReturnsFalse()
    {
        var entry = AuditEntry.Create(TontineId.Create(), VersementId.Create(), AuditAction.VersementCree, "actor", "payload", AuditEntry.GenesisHash);

        entry.VerifyIntegrity("tampered_hash").Should().BeFalse();
    }

    [Fact]
    public void AuditEntry_Chain_TwoEntries_IntegrityMaintained()
    {
        var tontineId = TontineId.Create();
        var versementId = VersementId.Create();

        var entry1 = AuditEntry.Create(tontineId, versementId, AuditAction.VersementCree, "actor", "p1", AuditEntry.GenesisHash);
        var entry2 = AuditEntry.Create(tontineId, versementId, AuditAction.VersementConfirme, "actor", "p2", entry1.HashCourant);

        entry1.VerifyIntegrity(AuditEntry.GenesisHash).Should().BeTrue();
        entry2.VerifyIntegrity(entry1.HashCourant).Should().BeTrue();
    }

    // ─── VersementId, PayeurId, TourId Value Objects ───────────────────────

    [Fact]
    public void VersementId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => VersementId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PayeurId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => PayeurId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TourId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => TourId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditEntryId_From_EmptyGuid_ThrowsArgumentException()
    {
        var act = () => AuditEntryId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VersementId_Create_GeneratesUniqueIds()
    {
        var id1 = VersementId.Create();
        var id2 = VersementId.Create();

        id1.Should().NotBe(id2);
    }

    // ─── AuditTrail as ReadOnlyCollection ──────────────────────────────────

    [Fact]
    public void AuditTrail_IsReadOnlyCollection()
    {
        var versement = CreateDefaultVersement();

        versement.AuditTrail.Should().BeAssignableTo<IReadOnlyCollection<AuditEntry>>();
    }
}
