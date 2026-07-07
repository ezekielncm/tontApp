using Application.Common;
using Application.TontineManagement.Commands.ActivateTontine;
using Domain.Common;
using Domain.TontineManagement;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using Domain.IdentityManagement.ValueObjects;
using Moq;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Application.UnitTests.TontineManagement.Commands.ActivateTontine
{
    public class ActivateTontineCommandHandlerTests
    {
        private readonly Mock<ITontineRepository> _tontineRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly ActivateTontineCommandHandler _handler;

        public ActivateTontineCommandHandlerTests()
        {
            _tontineRepositoryMock = new Mock<ITontineRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _handler = new ActivateTontineCommandHandler(_tontineRepositoryMock.Object, _unitOfWorkMock.Object);
        }

        private Tontine CreateTontine()
        {
            var amount = ContributionAmount.Create(100, "XOF");
            return Tontine.Create(
                "Test Tontine",
                "Description",
                amount,
                TontinePeriodicity.Monthly,
                10,
                UtilisateurId.Create(),
                ModeAttribution.Aleatoire
            );
        }

        [Fact]
        public async Task Handle_WithValidTontine_ActivatesAndSaves()
        {
            // Arrange
            var tontine = CreateTontine();
            var tontineId = tontine.Id;
            var command = new ActivateTontineCommand(tontineId.Value);

            // Add minimum members to allow activation
            tontine.JoinWithInvitation("Membre 1", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create());
            tontine.JoinWithInvitation("Membre 2", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create());
            tontine.JoinWithInvitation("Membre 3", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create()); // Ensure we have enough members based on Reglement defaults (3)

            _tontineRepositoryMock
                .Setup(r => r.GetByIdAsync(It.Is<TontineId>(id => id.Value == tontineId.Value), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tontine);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            tontine.Status.Should().Be(TontineStatus.Active);

            _tontineRepositoryMock.Verify(r => r.UpdateAsync(tontine, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTontineNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var tontineId = Guid.NewGuid();
            var command = new ActivateTontineCommand(tontineId);

            _tontineRepositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<TontineId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tontine?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Tontine {tontineId} not found.");

            _tontineRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tontine>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenTontineNotDraft_ThrowsInvalidOperationException()
        {
            // Arrange
            var tontine = CreateTontine();
            var tontineId = tontine.Id;
            var command = new ActivateTontineCommand(tontineId.Value);

            // Add minimum members and activate
            tontine.JoinWithInvitation("Membre 1", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create());
            tontine.JoinWithInvitation("Membre 2", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create());
            tontine.JoinWithInvitation("Membre 3", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create()); // Need 3 for default Reglement.MinMembresActivation
            tontine.Activate(); // Change status to Active

            _tontineRepositoryMock
                .Setup(r => r.GetByIdAsync(It.Is<TontineId>(id => id.Value == tontineId.Value), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tontine);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Only a Draft tontine can be activated.");

            // Ensure we don't save a failed activation attempt
            _tontineRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tontine>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenNotEnoughMembers_ThrowsInvalidOperationException()
        {
            // Arrange
            var tontine = CreateTontine();
            var tontineId = tontine.Id;
            var command = new ActivateTontineCommand(tontineId.Value);

            // Only add 1 member (less than minimum 3)
            tontine.JoinWithInvitation("Membre 1", tontine.GenerateInvitation().PlainCode, UtilisateurId.Create());

            _tontineRepositoryMock
                .Setup(r => r.GetByIdAsync(It.Is<TontineId>(id => id.Value == tontineId.Value), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tontine);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("A tontine must have at least 3 members to activate.");

            _tontineRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tontine>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
