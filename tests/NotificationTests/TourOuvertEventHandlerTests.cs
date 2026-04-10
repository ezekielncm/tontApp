using Application.NotificationManagement.EventHandlers;
using Application.NotificationManagement.Services;
using Domain.IdentityManagement.ValueObjects;
using Domain.NotificationManagement.ValueObjects;
using Domain.TontineManagement;
using Domain.TontineManagement.Events;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace NotificationTests;

public class TourOuvertEventHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ITontineRepository> _tontineRepositoryMock;
    private readonly Mock<ILogger<TourOuvertEventHandler>> _loggerMock;
    private readonly TourOuvertEventHandler _handler;

    public TourOuvertEventHandlerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _tontineRepositoryMock = new Mock<ITontineRepository>();
        _loggerMock = new Mock<ILogger<TourOuvertEventHandler>>();

        _handler = new TourOuvertEventHandler(
            _notificationServiceMock.Object,
            _tontineRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PlanifiesNotification()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var roundId = RoundId.Create();
        var beneficiaryId = MemberId.Create();
        var roundNumber = 3;

        var evt = new RoundOpenedEvent(tontineId, roundId, beneficiaryId, roundNumber);

        var tontine = CreateTontine(tontineId, "Ma Tontine Epargne");
        _tontineRepositoryMock
            .Setup(r => r.GetByIdReadOnlyAsync(tontineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tontine);

        _notificationServiceMock
            .Setup(s => s.PlanifierNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(s => s.PlanifierNotificationAsync(
            beneficiaryId.Value.ToString(),
            NotificationType.OuvertureTour,
            It.Is<string>(msg => msg.Contains("Ma Tontine Epargne") && msg.Contains("3")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTontineNotFound_UsesFallbackName()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var evt = new RoundOpenedEvent(tontineId, RoundId.Create(), MemberId.Create(), 1);

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
            NotificationType.OuvertureTour,
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
            UtilisateurId.Create(),
            ModeAttribution.Sequentiel);

        // Use reflection to set the Id since it's generated in Create
        var idProperty = typeof(Domain.Common.Entity<TontineId>).GetProperty("Id");
        idProperty?.SetValue(tontine, id);

        return tontine;
    }
}
