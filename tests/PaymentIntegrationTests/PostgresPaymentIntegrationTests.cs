namespace PaymentIntegrationTests;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Ports;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;

/// <summary>
/// Integration tests for the Payment module using TestContainers with a real PostgreSQL database.
/// Tests cover: persistence, webhook simulation, audit trail integrity, and repository queries.
/// Each test class gets a fresh database container for isolation.
/// </summary>
[Collection("PostgresIntegration")]
public class PostgresPaymentIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private PaymentTestDbContext _dbContext = null!;
    private IVersementRepository _versementRepo = null!;
    private IAuditEntryRepository _auditEntryRepo = null!;
    private readonly Mock<IMediator> _mediatorMock = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<PaymentTestDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _dbContext = new PaymentTestDbContext(options, _mediatorMock.Object);
        await _dbContext.Database.EnsureCreatedAsync();

        _versementRepo = new TestVersementRepository(_dbContext);
        _auditEntryRepo = new TestAuditEntryRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task SaveAndDetach()
    {
        await _dbContext.SaveChangesAsync();
        // Detach all tracked entities to simulate fresh reads
        foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }

    // ─── 1. Versement persistence ──────────────────────────────────────────

    [Fact]
    public async Task AddAsync_Versement_PersistsAndRetrieves()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));

        // Act
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(versement.Id);
        retrieved.Montant.Valeur.Should().Be(500m);
        retrieved.Montant.Devise.Should().Be("XOF");
        retrieved.Statut.Should().Be(VersementStatus.EnAttente);
    }

    [Fact]
    public async Task AddAsync_Versement_PersistsAuditTrail()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(1000m));

        // Act
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.AuditTrail.Should().ContainSingle();
        var entry = retrieved.AuditTrail.First();
        entry.Action.Should().Be(AuditAction.VersementCree);
        entry.HashPrecedent.Should().Be(AuditEntry.GenesisHash);
    }

    // ─── 2. Confirm versement and audit trail ──────────────────────────────

    [Fact]
    public async Task Confirmer_Versement_PersistsStatusAndAuditTrail()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        // Act - Simulate Orange Money webhook confirmation
        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-OM-12345");
        await SaveAndDetach();

        // Assert
        var confirmed = await _versementRepo.GetByIdAsync(versement.Id);
        confirmed.Should().NotBeNull();
        confirmed!.Statut.Should().Be(VersementStatus.Confirme);
        confirmed.ReferenceExterne.Should().Be("TXN-OM-12345");
        confirmed.ConfirmedAt.Should().NotBeNull();
        confirmed.AuditTrail.Should().HaveCount(2);
        confirmed.AuditTrail.Last().Action.Should().Be(AuditAction.VersementConfirme);
    }

    // ─── 3. Reject versement and audit trail ───────────────────────────────

    [Fact]
    public async Task Rejeter_Versement_PersistsStatusAndAuditTrail()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        // Act - Simulate Orange Money webhook rejection
        var toReject = await _versementRepo.GetByIdAsync(versement.Id);
        toReject!.Rejeter("Insufficient funds");
        await SaveAndDetach();

        // Assert
        var rejected = await _versementRepo.GetByIdAsync(versement.Id);
        rejected.Should().NotBeNull();
        rejected!.Statut.Should().Be(VersementStatus.Echoue);
        rejected.AuditTrail.Should().HaveCount(2);
        rejected.AuditTrail.Last().Action.Should().Be(AuditAction.VersementRejete);
    }

    // ─── 4. Hash chain integrity after round-trip ──────────────────────────

    [Fact]
    public async Task HashValues_AfterPersistAndRetrieve_PreservedCorrectly()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        var originalHash = versement.HashCourant;
        var originalPrevHash = versement.HashPrecedent;

        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        // Act
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert - Hash values are preserved through round-trip
        retrieved.Should().NotBeNull();
        retrieved!.HashCourant.Should().Be(originalHash);
        retrieved.HashPrecedent.Should().Be(originalPrevHash);
        retrieved.HashCourant.Should().HaveLength(64); // SHA-256 hex length
        retrieved.AuditTrail.Should().ContainSingle();
    }

    [Fact]
    public async Task AuditTrail_AfterConfirmationRoundTrip_ChainPreserved()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-INTEGRITY-001");
        await SaveAndDetach();

        // Act
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert - Audit trail chain structure is preserved
        retrieved.Should().NotBeNull();
        retrieved!.AuditTrail.Should().HaveCount(2);
        var entries = retrieved.AuditTrail.ToList();
        entries[0].Action.Should().Be(AuditAction.VersementCree);
        entries[1].Action.Should().Be(AuditAction.VersementConfirme);
        entries[0].HashPrecedent.Should().Be(AuditEntry.GenesisHash);
        entries[1].HashPrecedent.Should().Be(entries[0].HashCourant);
    }

    // ─── 5. Hash chain across multiple versements ──────────────────────────

    [Fact]
    public async Task HashChain_MultipleVersements_LinkagePreserved()
    {
        // Arrange
        var tontineId = TontineId.Create();

        var v1 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m), "");
        await _versementRepo.AddAsync(v1);
        await SaveAndDetach();

        var v2 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(600m), v1.HashCourant);
        await _versementRepo.AddAsync(v2);
        await SaveAndDetach();

        var v3 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(700m), v2.HashCourant);
        await _versementRepo.AddAsync(v3);
        await SaveAndDetach();

        // Act
        var allVersements = await _versementRepo.GetByTontineAsync(tontineId);

        // Assert - Chain linkage preserved
        allVersements.Should().HaveCount(3);
        allVersements[1].HashPrecedent.Should().Be(allVersements[0].HashCourant);
        allVersements[2].HashPrecedent.Should().Be(allVersements[1].HashCourant);
        // All hash values are 64 chars (SHA-256)
        allVersements.Should().OnlyContain(v => v.HashCourant.Length == 64);
    }

    // ─── 6. GetByTontineAndTour ────────────────────────────────────────────

    [Fact]
    public async Task GetByTontineAndTour_FiltersCorrectly()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var tourId = TourId.Create();
        var otherTourId = TourId.Create();

        var v1 = Versement.Create(tontineId, tourId, PayeurId.Create(), Montant.Create(500m));
        var v2 = Versement.Create(tontineId, tourId, PayeurId.Create(), Montant.Create(600m));
        var v3 = Versement.Create(tontineId, otherTourId, PayeurId.Create(), Montant.Create(700m));

        await _versementRepo.AddAsync(v1);
        await _versementRepo.AddAsync(v2);
        await _versementRepo.AddAsync(v3);
        await SaveAndDetach();

        // Act
        var result = await _versementRepo.GetByTontineAndTourAsync(tontineId, tourId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.TourId == tourId);
    }

    // ─── 7. GetByPayeur ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPayeur_FiltersCorrectly()
    {
        // Arrange
        var payeurId = PayeurId.Create();
        var otherPayeurId = PayeurId.Create();

        var v1 = Versement.Create(TontineId.Create(), TourId.Create(), payeurId, Montant.Create(500m));
        var v2 = Versement.Create(TontineId.Create(), TourId.Create(), payeurId, Montant.Create(600m));
        var v3 = Versement.Create(TontineId.Create(), TourId.Create(), otherPayeurId, Montant.Create(700m));

        await _versementRepo.AddAsync(v1);
        await _versementRepo.AddAsync(v2);
        await _versementRepo.AddAsync(v3);
        await SaveAndDetach();

        // Act
        var result = await _versementRepo.GetByPayeurAsync(payeurId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.PayeurId == payeurId);
    }

    // ─── 8. GetLastByTontine ───────────────────────────────────────────────

    [Fact]
    public async Task GetLastByTontine_ReturnsNewest()
    {
        // Arrange
        var tontineId = TontineId.Create();

        var v1 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(v1);
        await SaveAndDetach();

        // Small delay to ensure different CreatedAt
        await Task.Delay(10);
        var v2 = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(600m));
        await _versementRepo.AddAsync(v2);
        await SaveAndDetach();

        // Act
        var last = await _versementRepo.GetLastByTontineAsync(tontineId);

        // Assert
        last.Should().NotBeNull();
        last!.Id.Should().Be(v2.Id);
    }

    // ─── 9. GetByReferenceExterne ──────────────────────────────────────────

    [Fact]
    public async Task GetByReferenceExterne_FindsConfirmedVersement()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-UNIQUE-REF");
        await SaveAndDetach();

        // Act
        var found = await _versementRepo.GetByReferenceExterneAsync("TXN-UNIQUE-REF");

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(versement.Id);
        found.ReferenceExterne.Should().Be("TXN-UNIQUE-REF");
    }

    [Fact]
    public async Task GetByReferenceExterne_NonExistent_ReturnsNull()
    {
        // Act
        var found = await _versementRepo.GetByReferenceExterneAsync("DOES-NOT-EXIST");

        // Assert
        found.Should().BeNull();
    }

    // ─── 10. Simulated Orange Money webhook flow ───────────────────────────

    [Fact]
    public async Task WebhookSimulation_FullFlow_CreateConfirmVerify()
    {
        // Step 1: Create versement (initiate payment)
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(5000m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        // Step 2: Simulate Orange Money webhook callback (confirmation)
        var pending = await _versementRepo.GetByIdAsync(versement.Id);
        pending!.Statut.Should().Be(VersementStatus.EnAttente);
        pending.Confirmer("ATK-TXN-98765");
        await SaveAndDetach();

        // Step 3: Verify final state
        var confirmed = await _versementRepo.GetByIdAsync(versement.Id);
        confirmed.Should().NotBeNull();
        confirmed!.Statut.Should().Be(VersementStatus.Confirme);
        confirmed.ReferenceExterne.Should().Be("ATK-TXN-98765");
        confirmed.ConfirmedAt.Should().NotBeNull();

        // Step 4: Verify audit trail integrity
        confirmed.AuditTrail.Should().HaveCount(2);
        var entries = confirmed.AuditTrail.ToList();
        entries[0].Action.Should().Be(AuditAction.VersementCree);
        entries[1].Action.Should().Be(AuditAction.VersementConfirme);
        entries[1].HashPrecedent.Should().Be(entries[0].HashCourant);
    }

    [Fact]
    public async Task WebhookSimulation_RejectionFlow_CreateRejectVerify()
    {
        // Step 1: Create versement
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(5000m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        // Step 2: Simulate Orange Money webhook rejection
        var pending = await _versementRepo.GetByIdAsync(versement.Id);
        pending!.Rejeter("Solde insuffisant");
        await SaveAndDetach();

        // Step 3: Verify final state
        var rejected = await _versementRepo.GetByIdAsync(versement.Id);
        rejected.Should().NotBeNull();
        rejected!.Statut.Should().Be(VersementStatus.Echoue);
        rejected.ReferenceExterne.Should().BeNull();
        rejected.ConfirmedAt.Should().BeNull();

        // Step 4: Verify audit trail chain structure
        rejected.AuditTrail.Should().HaveCount(2);
        var entries = rejected.AuditTrail.ToList();
        entries[0].Action.Should().Be(AuditAction.VersementCree);
        entries[1].Action.Should().Be(AuditAction.VersementRejete);
        entries[1].HashPrecedent.Should().Be(entries[0].HashCourant);
    }

    // ─── 11. Idempotence: duplicate confirmation ───────────────────────────

    [Fact]
    public async Task WebhookSimulation_DuplicateConfirmation_ThrowsAndPreservesState()
    {
        // Arrange - Create and confirm
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-FIRST");
        await SaveAndDetach();

        // Act - Try duplicate confirmation
        var confirmed = await _versementRepo.GetByIdAsync(versement.Id);
        var act = () => confirmed!.Confirmer("TXN-DUPLICATE");

        // Assert
        act.Should().Throw<InvalidOperationException>();
        confirmed!.ReferenceExterne.Should().Be("TXN-FIRST");
    }

    // ─── 12. AuditEntry repository queries ─────────────────────────────────

    [Fact]
    public async Task AuditEntryRepository_GetByTontineOrdered_ReturnsChronological()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-ORDER-TEST");
        await SaveAndDetach();

        // Act
        var entries = await _auditEntryRepo.GetByTontineOrderedAsync(tontineId);

        // Assert
        entries.Should().HaveCount(2);
        entries[0].Action.Should().Be(AuditAction.VersementCree);
        entries[1].Action.Should().Be(AuditAction.VersementConfirme);
        entries[0].Timestamp.Should().BeOnOrBefore(entries[1].Timestamp);
    }

    [Fact]
    public async Task AuditEntryRepository_GetByTontinePaged_ReturnsCorrectPage()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-PAGE-TEST");
        await SaveAndDetach();

        // Act - Get page 1 with size 1
        var page1 = await _auditEntryRepo.GetByTontinePagedAsync(tontineId, 1, 1);

        // Assert
        page1.Should().ContainSingle();
        page1[0].Action.Should().Be(AuditAction.VersementConfirme); // newest first (DESC order)
    }

    [Fact]
    public async Task AuditEntryRepository_CountByTontine_ReturnsCorrectCount()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-COUNT-TEST");
        await SaveAndDetach();

        // Act
        var count = await _auditEntryRepo.CountByTontineAsync(tontineId);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task AuditEntryRepository_GetLastByTontine_ReturnsNewest()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-LAST-TEST");
        await SaveAndDetach();

        // Act
        var last = await _auditEntryRepo.GetLastByTontineAsync(tontineId);

        // Assert
        last.Should().NotBeNull();
        last!.Action.Should().Be(AuditAction.VersementConfirme);
    }

    // ─── 13. Minimum amount boundary ───────────────────────────────────────

    [Fact]
    public async Task Versement_WithMinimumAmount_PersistsCorrectly()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(100m));

        // Act
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Montant.Valeur.Should().Be(100m);
    }

    // ─── 14. Large amount ──────────────────────────────────────────────────

    [Fact]
    public async Task Versement_WithLargeAmount_PersistsCorrectly()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(999_999_999.99m));

        // Act
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Montant.Valeur.Should().Be(999_999_999.99m);
    }

    // ─── 15. Hash values persisted correctly ───────────────────────────────

    [Fact]
    public async Task HashValues_PersistCorrectly_AcrossRoundTrip()
    {
        // Arrange
        var versement = Versement.Create(TontineId.Create(), TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        var originalHashCourant = versement.HashCourant;
        var originalHashPrecedent = versement.HashPrecedent;

        // Act
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();
        var retrieved = await _versementRepo.GetByIdAsync(versement.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.HashCourant.Should().Be(originalHashCourant);
        retrieved.HashPrecedent.Should().Be(originalHashPrecedent);
        retrieved.HashCourant.Should().HaveLength(64);
    }

    // ─── 16. Empty tontine queries ─────────────────────────────────────────

    [Fact]
    public async Task GetByTontine_EmptyTontine_ReturnsEmptyList()
    {
        // Act
        var result = await _versementRepo.GetByTontineAsync(TontineId.Create());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLastByTontine_EmptyTontine_ReturnsNull()
    {
        // Act
        var result = await _versementRepo.GetLastByTontineAsync(TontineId.Create());

        // Assert
        result.Should().BeNull();
    }

    // ─── 17. Audit chain integrity across operations ───────────────────────

    [Fact]
    public async Task AuditChain_FullFlow_AllEntriesChainedCorrectly()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var versement = Versement.Create(tontineId, TourId.Create(), PayeurId.Create(), Montant.Create(500m));
        await _versementRepo.AddAsync(versement);
        await SaveAndDetach();

        var toConfirm = await _versementRepo.GetByIdAsync(versement.Id);
        toConfirm!.Confirmer("TXN-CHAIN-TEST");
        await SaveAndDetach();

        // Act - Retrieve all audit entries
        var entries = await _auditEntryRepo.GetByTontineOrderedAsync(tontineId);

        // Assert - Verify the chain
        entries.Should().HaveCount(2);

        var firstEntry = entries[0];
        var secondEntry = entries[1];

        // First entry should chain from genesis hash
        firstEntry.HashPrecedent.Should().Be(AuditEntry.GenesisHash);
        firstEntry.HashCourant.Should().NotBeNullOrEmpty();

        // Second entry should chain from first
        secondEntry.HashPrecedent.Should().Be(firstEntry.HashCourant);
        secondEntry.HashCourant.Should().NotBeNullOrEmpty();
        secondEntry.HashCourant.Should().NotBe(firstEntry.HashCourant);
    }
}
