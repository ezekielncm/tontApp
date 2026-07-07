namespace PaymentHandlerTests;

using Application.PaymentManagement.Commands.RejeterVersement;
using Domain.Common;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Moq;
using FluentAssertions;

public class RejeterVersementCommandHandlerTests
{
    private readonly Mock<IVersementRepository> _versementRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private RejeterVersementCommandHandler CreateHandler() =>
        new(
            _versementRepositoryMock.Object,
            _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_RejetsVersement_WhenVersementExistsAndIsPending()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var tourId = TourId.Create();
        var payeurId = PayeurId.Create();
        var montant = Montant.Create(100m, "XAF");

        var versement = Versement.Create(tontineId, tourId, payeurId, montant);
        var raison = "Funds missing";
        var command = new RejeterVersementCommand(versement.Id.Value, raison);
        var handler = CreateHandler();

        _versementRepositoryMock
            .Setup(repo => repo.GetByIdAsync(versement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        versement.Statut.Should().Be(VersementStatus.Echoue);

        _versementRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.Is<Versement>(v => v.Id == versement.Id && v.Statut == VersementStatus.Echoue), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenVersementNotFound()
    {
        // Arrange
        var versementId = Guid.NewGuid();
        var command = new RejeterVersementCommand(versementId, "Funds missing");
        var handler = CreateHandler();

        _versementRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<VersementId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Versement?)null);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Versement {versementId} not found.");

        _versementRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.IsAny<Versement>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenVersementNotPending()
    {
        // Arrange
        var tontineId = TontineId.Create();
        var tourId = TourId.Create();
        var payeurId = PayeurId.Create();
        var montant = Montant.Create(100m, "XAF");

        var versement = Versement.Create(tontineId, tourId, payeurId, montant);
        versement.Confirmer("EXT_REF_123"); // Changes status to Confirme

        var command = new RejeterVersementCommand(versement.Id.Value, "Funds missing");
        var handler = CreateHandler();

        _versementRepositoryMock
            .Setup(repo => repo.GetByIdAsync(versement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versement);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a pending versement can be rejected.");

        _versementRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.IsAny<Versement>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
