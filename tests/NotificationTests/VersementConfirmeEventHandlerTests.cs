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

public class VersementConfirmeEventHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ITontineRepository> _tontineRepositoryMock;
    private readonly Mock<ILogger<VersementConfirmeEventHandler>> _loggerMock;
    private readonly VersementConfirmeEventHandler _handler;

    public VersementConfirmeEventHandlerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _tontineRepositoryMock = new Mock<ITontineRepository>();
        _loggerMock = new Mock<ILogger<VersementConfirmeEventHandler>>();

        _handler = new VersementConfirmeEventHandler(
            _notificationServiceMock.Object,
            _tontineRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PlanifiesConfirmationSms()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new VersementConfirmedEvent(
            VersementId.From(Guid.NewGuid()),
            tontineId,
            payeurId,
            TourId.From(Guid.NewGuid()),
            5000m,
            "REF-123");

        var tontine = CreateTontine(tontineId, "Tontine Solidaire");
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
            NotificationType.ConfirmationPaiement,
            It.Is<string>(msg => msg.Contains("5000") && msg.Contains("Tontine Solidaire")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTontineNotFound_UsesFallbackName()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new VersementConfirmedEvent(
            VersementId.From(Guid.NewGuid()),
            tontineId,
            payeurId,
            TourId.From(Guid.NewGuid()),
            3000m,
            "REF-456");

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
            payeurId.Value.ToString(),
            NotificationType.ConfirmationPaiement,
            It.Is<string>(msg => msg.Contains("votre tontine")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AlwaysSendsConfirmation_RegardlessOfRateLimit()
    {
        // Confirmation de paiement = toujours envoyée (même si opt-out)
        var tontineId = TontineId.Create();
        var payeurId = PayeurId.From(Guid.NewGuid());

        var evt = new VersementConfirmedEvent(
            VersementId.From(Guid.NewGuid()),
            tontineId,
            payeurId,
            TourId.From(Guid.NewGuid()),
            1000m,
            "REF-789");

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

        // Assert - notification type is ConfirmationPaiement which is critical
        _notificationServiceMock.Verify(s => s.PlanifierNotificationAsync(
            It.IsAny<string>(),
            NotificationType.ConfirmationPaiement,
            It.IsAny<string>(),
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
