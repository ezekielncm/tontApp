using Application.NotificationManagement.EventHandlers;
using Application.NotificationManagement.Services;
using Domain.NotificationManagement.ValueObjects;
using Domain.PaymentManagement.Events;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace NotificationTests;

public class PaiementEnRetardEventHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ITontineRepository> _tontineRepositoryMock;
    private readonly Mock<ILogger<PaiementEnRetardEventHandler>> _loggerMock;
    private readonly PaiementEnRetardEventHandler _handler;

    public PaiementEnRetardEventHandlerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _tontineRepositoryMock = new Mock<ITontineRepository>();
        _loggerMock = new Mock<ILogger<PaiementEnRetardEventHandler>>();

        _handler = new PaiementEnRetardEventHandler(
            _notificationServiceMock.Object,
            _tontineRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PlanifiesRappelSms()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new PaiementEnRetardEvent(
            tontineId,
            TourId.From(Guid.NewGuid()),
            payeurId,
            5000m,
            "XOF");

        var tontine = CreateTontine(tontineId, "Tontine Entraide");
        _tontineRepositoryMock
            .Setup(r => r.GetByIdReadOnlyAsync(tontineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tontine);

        _notificationServiceMock
            .Setup(s => s.PlanifierNotificationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(s => s.PlanifierNotificationAsync(
            payeurId.Value.ToString(),
            NotificationType.RappelPaiement,
            It.Is<string>(msg => msg.Contains("retard") && msg.Contains("Tontine Entraide")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_IncludesMontantAndDevise_InMessage()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new PaiementEnRetardEvent(
            tontineId,
            TourId.From(Guid.NewGuid()),
            payeurId,
            7500m,
            "XOF");

        _tontineRepositoryMock
            .Setup(r => r.GetByIdReadOnlyAsync(tontineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tontine?)null);

        _notificationServiceMock
            .Setup(s => s.PlanifierNotificationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(s => s.PlanifierNotificationAsync(
            It.IsAny<string>(),
            NotificationType.RappelPaiement,
            It.Is<string>(msg => msg.Contains("7500") && msg.Contains("XOF")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTontineNotFound_UsesFallbackName()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new PaiementEnRetardEvent(
            tontineId,
            TourId.From(Guid.NewGuid()),
            payeurId,
            2000m,
            "XOF");

        _tontineRepositoryMock
            .Setup(r => r.GetByIdReadOnlyAsync(tontineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tontine?)null);

        _notificationServiceMock
            .Setup(s => s.PlanifierNotificationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(s => s.PlanifierNotificationAsync(
            It.IsAny<string>(),
            NotificationType.RappelPaiement,
            It.Is<string>(msg => msg.Contains("votre tontine")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Tontine CreateTontine(TontineId id, string name)
    {
        var tontine = Tontine.Create(
            name,
            "Test description",
            ContributionAmount.Create(5000, "XOF"),
            TontinePeriodicity.Monthly,
            10,
            ModeAttribution.Sequentiel);

        var idProperty = typeof(Domain.Common.Entity<TontineId>).GetProperty("Id");
        idProperty?.SetValue(tontine, id);

        return tontine;
    }
}
