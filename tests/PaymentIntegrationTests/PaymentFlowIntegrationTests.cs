namespace PaymentIntegrationTests;

using Application.PaymentManagement.Commands.ConfirmVersement;
using Application.PaymentManagement.Commands.InitierVersement;
using Application.PaymentManagement.Commands.RejeterVersement;
using Domain.Common;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Ports;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Moq;

/// <summary>
/// Integration tests for the payment flow with mock gateway.
/// Tests confirmation, rejection, idempotence, and audit trail integrity.
/// </summary>
public class PaymentFlowIntegrationTests
{
    private readonly Mock<IVersementRepository> _versementRepoMock;
    private readonly Mock<IMobileMoneyGateway> _gatewayMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public PaymentFlowIntegrationTests()
    {
        _versementRepoMock = new Mock<IVersementRepository>();
        _gatewayMock = new Mock<IMobileMoneyGateway>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task InitierVersement_WithValidData_CreatesVersementAndInitiatesPayment()
    {
        // Arrange
        _versementRepoMock
            .Setup(r => r.GetLastByTontineAsync(It.IsAny<TontineId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Versement?)null);

        _gatewayMock
            .Setup(g => g.InitierPaiementAsync(It.IsAny<MobileMoneyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileMoneyResponse(true, "TXN-001", "PendingConfirmation"));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new InitierVersementCommandHandler(
            _versementRepoMock.Object,
            _gatewayMock.Object,
            _unitOfWorkMock.Object);

        var command = new InitierVersementCommand(
            TontineId: Guid.NewGuid(),
            TourId: Guid.NewGuid(),
            PayeurId: Guid.NewGuid(),
            NumeroTelephone: "+22670000000",
            Montant: 500m,
            Devise: "XOF");

        // Act
        var versementId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, versementId);

        _versementRepoMock.Verify(r => r.AddAsync(
            It.Is<Versement>(v =>
                v.Statut == VersementStatus.EnAttente &&
                v.Montant.Valeur == 500m &&
                v.Montant.Devise == "XOF"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _gatewayMock.Verify(g => g.InitierPaiementAsync(
            It.Is<MobileMoneyRequest>(r =>
                r.NumeroTelephone == "+22670000000" &&
                r.Montant == 500m &&
                r.Devise == "XOF"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitierVersement_WithChainedHash_UsesLastVersementHash()
    {
        // Arrange
        var previousVersement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(200m));

        var tontineId = previousVersement.TontineId;

        _versementRepoMock
            .Setup(r => r.GetLastByTontineAsync(It.IsAny<TontineId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousVersement);

        _gatewayMock
            .Setup(g => g.InitierPaiementAsync(It.IsAny<MobileMoneyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileMoneyResponse(true, "TXN-002", "PendingConfirmation"));

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new InitierVersementCommandHandler(
            _versementRepoMock.Object,
            _gatewayMock.Object,
            _unitOfWorkMock.Object);

        var command = new InitierVersementCommand(
            TontineId: tontineId.Value,
            TourId: Guid.NewGuid(),
            PayeurId: Guid.NewGuid(),
            NumeroTelephone: "+22670000001",
            Montant: 300m,
            Devise: "XOF");

        // Act
        var versementId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, versementId);

        _versementRepoMock.Verify(r => r.AddAsync(
            It.Is<Versement>(v =>
                v.HashPrecedent == previousVersement.HashCourant &&
                !string.IsNullOrEmpty(v.HashCourant) &&
                v.HashCourant != v.HashPrecedent),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmerVersement_WithValidId_SetsStatusConfirme()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        _versementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ConfirmVersementCommandHandler(
            _versementRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new ConfirmVersementCommand(versement.Id.Value, "TXN-ORANGE-123");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(VersementStatus.Confirme, versement.Statut);
        Assert.Equal("TXN-ORANGE-123", versement.ReferenceExterne);
        Assert.NotNull(versement.ConfirmedAt);
        Assert.True(versement.VerifierIntegrite());
    }

    [Fact]
    public async Task RejeterVersement_WithValidId_SetsStatusEchoue()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        _versementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RejeterVersementCommandHandler(
            _versementRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new RejeterVersementCommand(versement.Id.Value, "Insufficient funds");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(VersementStatus.Echoue, versement.Statut);
        Assert.True(versement.VerifierIntegrite());
    }

    [Fact]
    public async Task ConfirmerVersement_AlreadyConfirmed_ThrowsInvalidOperationException()
    {
        // Arrange - Idempotence test
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        versement.Confirmer("TXN-FIRST");

        _versementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ConfirmVersementCommandHandler(
            _versementRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new ConfirmVersementCommand(versement.Id.Value, "TXN-DUPLICATE");

        // Act & Assert - Idempotence: second confirmation throws
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify that the first confirmation is preserved
        Assert.Equal(VersementStatus.Confirme, versement.Statut);
        Assert.Equal("TXN-FIRST", versement.ReferenceExterne);
    }

    [Fact]
    public async Task RejeterVersement_AlreadyRejected_ThrowsInvalidOperationException()
    {
        // Arrange - Idempotence test
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        versement.Rejeter("First failure");

        _versementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RejeterVersementCommandHandler(
            _versementRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new RejeterVersementCommand(versement.Id.Value, "Second failure");

        // Act & Assert - Duplicate rejection throws
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmerVersement_NotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _versementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Versement?)null);

        var handler = new ConfirmVersementCommandHandler(
            _versementRepoMock.Object,
            _unitOfWorkMock.Object);

        var command = new ConfirmVersementCommand(Guid.NewGuid(), "TXN-UNKNOWN");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Montant_BelowMinimum_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Montant.Create(99m, "XOF"));
        Assert.Throws<ArgumentException>(() => Montant.Create(50m, "XOF"));
        Assert.Throws<ArgumentException>(() => Montant.Create(0m, "XOF"));
        Assert.Throws<ArgumentException>(() => Montant.Create(-100m, "XOF"));
    }

    [Fact]
    public void Montant_AtMinimum_Succeeds()
    {
        var montant = Montant.Create(100m, "XOF");

        Assert.Equal(100m, montant.Valeur);
        Assert.Equal("XOF", montant.Devise);
    }

    [Fact]
    public void AuditTrail_IntegrityVerification_AfterConfirmation()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(1000m));

        // Act
        versement.Confirmer("TXN-AUDIT-TEST");

        // Assert
        Assert.True(versement.VerifierIntegrite());
        Assert.Equal(2, versement.AuditTrail.Count);

        var firstEntry = versement.AuditTrail.First();
        var lastEntry = versement.AuditTrail.Last();

        Assert.Equal("VersementCree", firstEntry.Action);
        Assert.Equal("VersementConfirme", lastEntry.Action);
        Assert.Equal(string.Empty, firstEntry.PreviousHash);
        Assert.Equal(firstEntry.Hash, lastEntry.PreviousHash);
    }

    [Fact]
    public void AuditTrail_IntegrityVerification_AfterRejection()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(1000m));

        // Act
        versement.Rejeter("Insufficient funds");

        // Assert
        Assert.True(versement.VerifierIntegrite());
        Assert.Equal(2, versement.AuditTrail.Count);

        var lastEntry = versement.AuditTrail.Last();
        Assert.Equal("VersementRejete", lastEntry.Action);
    }

    [Fact]
    public void CalculerHash_IsDeterministic()
    {
        var id = VersementId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var montant = Montant.Create(500m, "XOF");
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hashPrecedent = "abc123";

        var hash1 = Versement.CalculerHash(id, montant, date, hashPrecedent);
        var hash2 = Versement.CalculerHash(id, montant, date, hashPrecedent);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex string length
    }

    [Fact]
    public void CalculerHash_DifferentInputs_ProduceDifferentHashes()
    {
        var id = VersementId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var montant1 = Montant.Create(500m, "XOF");
        var montant2 = Montant.Create(600m, "XOF");
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var hash1 = Versement.CalculerHash(id, montant1, date, "");
        var hash2 = Versement.CalculerHash(id, montant2, date, "");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashChain_IsConsistent_AcrossVersements()
    {
        // Arrange - simulate a chain of versements
        var tontineId = TontineId.From(Guid.NewGuid());

        var v1 = Versement.Create(
            tontineId,
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m),
            ""); // first in chain

        var v2 = Versement.Create(
            tontineId,
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m),
            v1.HashCourant); // chains to v1

        // Assert
        Assert.Equal(string.Empty, v1.HashPrecedent);
        Assert.Equal(v1.HashCourant, v2.HashPrecedent);
        Assert.NotEqual(v1.HashCourant, v2.HashCourant);
        Assert.True(v1.VerifierIntegrite());
        Assert.True(v2.VerifierIntegrite());
    }
}
