namespace PaymentHandlerTests;

using Application.PaymentManagement.Commands.CreateVersement;
using Domain.Common;
using Domain.PaymentManagement;
using Domain.PaymentManagement.Repositories;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;
using Moq;
using Xunit;

public class CreateVersementCommandHandlerTests
{
    private readonly Mock<IVersementRepository> _versementRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateVersementCommandHandler _handler;

    public CreateVersementCommandHandlerTests()
    {
        _versementRepositoryMock = new Mock<IVersementRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateVersementCommandHandler(_versementRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_FirstVersement_CreatesAndSavesVersementWithEmptyHash()
    {
        // Arrange
        var command = new CreateVersementCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m);

        _versementRepositoryMock.Setup(repo => repo.GetLastByTontineAsync(It.IsAny<TontineId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Versement?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _versementRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Versement>(v =>
            v.TontineId.Value == command.TontineId &&
            v.TourId.Value == command.TourId &&
            v.PayeurId.Value == command.PayeurId &&
            v.Montant.Valeur == command.Montant &&
            v.HashPrecedent == string.Empty
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentVersement_CreatesAndSavesVersementWithPreviousHash()
    {
        // Arrange
        var tontineId = Guid.NewGuid();
        var command = new CreateVersementCommand(tontineId, Guid.NewGuid(), Guid.NewGuid(), 1000m);

        var lastVersement = Versement.Create(
            TontineId.From(tontineId),
            TourId.From(Guid.NewGuid()),
            PayeurId.From(Guid.NewGuid()),
            Montant.Create(500m, "XOF"),
            "previous_hash"
        );

        _versementRepositoryMock.Setup(repo => repo.GetLastByTontineAsync(It.Is<TontineId>(id => id.Value == tontineId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastVersement);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _versementRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Versement>(v =>
            v.TontineId.Value == command.TontineId &&
            v.HashPrecedent == lastVersement.HashCourant
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
