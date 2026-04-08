namespace PaymentIntegrationTests;

using Domain.PaymentManagement;
using Domain.PaymentManagement.Ports;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Moq;

/// <summary>
/// Tests for the Mobile Money gateway interactions.
/// Simulates both successful and failed payment scenarios.
/// </summary>
public class MobileMoneyGatewayTests
{
    [Fact]
    public async Task Gateway_InitierPaiement_Success_ReturnsTransactionId()
    {
        // Arrange
        var gatewayMock = new Mock<IMobileMoneyGateway>();
        gatewayMock
            .Setup(g => g.InitierPaiementAsync(It.IsAny<MobileMoneyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileMoneyResponse(true, "TXN-OM-12345", "PendingConfirmation"));

        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", Guid.NewGuid().ToString());

        // Act
        var response = await gatewayMock.Object.InitierPaiementAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal("TXN-OM-12345", response.TransactionId);
        Assert.Equal("PendingConfirmation", response.Description);
    }

    [Fact]
    public async Task Gateway_InitierPaiement_Failure_ReturnsError()
    {
        // Arrange
        var gatewayMock = new Mock<IMobileMoneyGateway>();
        gatewayMock
            .Setup(g => g.InitierPaiementAsync(It.IsAny<MobileMoneyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileMoneyResponse(false, null, "Insufficient funds"));

        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", Guid.NewGuid().ToString());

        // Act
        var response = await gatewayMock.Object.InitierPaiementAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Null(response.TransactionId);
        Assert.Equal("Insufficient funds", response.Description);
    }

    [Fact]
    public async Task Gateway_InitierPaiement_Timeout_ReturnsTimeout()
    {
        // Arrange - Simulate a timeout (10 second max as per constraints)
        var gatewayMock = new Mock<IMobileMoneyGateway>();
        gatewayMock
            .Setup(g => g.InitierPaiementAsync(It.IsAny<MobileMoneyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileMoneyResponse(false, null, "Request timed out after 10 seconds."));

        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", Guid.NewGuid().ToString());

        // Act
        var response = await gatewayMock.Object.InitierPaiementAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Null(response.TransactionId);
        Assert.Contains("timed out", response.Description);
    }

    [Fact]
    public void Versement_FullFlowWithGateway_Confirmation()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        // Assert initial state
        Assert.Equal(VersementStatus.EnAttente, versement.Statut);
        Assert.Single(versement.AuditTrail);

        // Act - Simulate webhook confirmation
        versement.Confirmer("TXN-OM-12345");

        // Assert final state
        Assert.Equal(VersementStatus.Confirme, versement.Statut);
        Assert.Equal("TXN-OM-12345", versement.ReferenceExterne);
        Assert.NotNull(versement.ConfirmedAt);
        Assert.Equal(2, versement.AuditTrail.Count);
        Assert.True(versement.VerifierIntegrite());
    }

    [Fact]
    public void Versement_FullFlowWithGateway_Rejection()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        // Assert initial state
        Assert.Equal(VersementStatus.EnAttente, versement.Statut);

        // Act - Simulate webhook rejection
        versement.Rejeter("Insufficient funds");

        // Assert final state
        Assert.Equal(VersementStatus.Echoue, versement.Statut);
        Assert.Null(versement.ReferenceExterne);
        Assert.Null(versement.ConfirmedAt);
        Assert.Equal(2, versement.AuditTrail.Count);
        Assert.True(versement.VerifierIntegrite());
    }

    [Fact]
    public void Versement_CannotConfirmAfterRejection()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        versement.Rejeter("Failed");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => versement.Confirmer("TXN-LATE"));
    }

    [Fact]
    public void Versement_CannotRejectAfterConfirmation()
    {
        // Arrange
        var versement = Versement.Create(
            TontineId.From(Guid.NewGuid()),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m));

        versement.Confirmer("TXN-OK");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => versement.Rejeter("Too late"));
    }
}
