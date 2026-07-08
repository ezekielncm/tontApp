using System;
using System.Threading;
using System.Threading.Tasks;
using Application.BillingManagement.Commands.CreateAbonnement;
using Application.BillingManagement.Services;
using Domain.BillingManagement;
using Domain.BillingManagement.Repositories;
using Domain.BillingManagement.ValueObjects;
using Domain.Common;
using Moq;
using Xunit;

namespace ApplicationTests.BillingManagement.Commands.CreateAbonnement
{
    public class CreateAbonnementCommandHandlerTests
    {
        private readonly Mock<IAbonnementRepository> _abonnementRepositoryMock;
        private readonly Mock<IPlanAbonnementRepository> _planRepositoryMock;
        private readonly Mock<IBillingCacheService> _billingCacheMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly CreateAbonnementCommandHandler _handler;

        public CreateAbonnementCommandHandlerTests()
        {
            _abonnementRepositoryMock = new Mock<IAbonnementRepository>();
            _planRepositoryMock = new Mock<IPlanAbonnementRepository>();
            _billingCacheMock = new Mock<IBillingCacheService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _handler = new CreateAbonnementCommandHandler(
                _abonnementRepositoryMock.Object,
                _planRepositoryMock.Object,
                _billingCacheMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommandAndPlanFound_ShouldCreateAbonnementAndUpdateCache()
        {
            // Arrange
            var gestionnaireId = "gest-123";
            var planStr = "Pro";
            var command = new CreateAbonnementCommand(gestionnaireId, planStr);

            var plan = PlanAbonnement.Create("Pro", "PRO", 1000m, "XOF", 10, 100);

            _planRepositoryMock
                .Setup(x => x.GetByCodeAsync("PRO", It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);

            _abonnementRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Abonnement>(a => a.GestionnaireId == gestionnaireId && a.Plan == PlanTarifaire.Pro),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _billingCacheMock.Verify(
                x => x.SetPlanLimitsAsync(
                    gestionnaireId,
                    plan.MaxTontines,
                    plan.MaxMembresParTontine,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidCommandAndPlanNotFound_ShouldCreateAbonnementButNotUpdateCache()
        {
            // Arrange
            var gestionnaireId = "gest-123";
            var planStr = "Gratuit";
            var command = new CreateAbonnementCommand(gestionnaireId, planStr);

            _planRepositoryMock
                .Setup(x => x.GetByCodeAsync("GRATUIT", It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlanAbonnement)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);

            _abonnementRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Abonnement>(a => a.GestionnaireId == gestionnaireId && a.Plan == PlanTarifaire.Gratuit),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _billingCacheMock.Verify(
                x => x.SetPlanLimitsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidPlan_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new CreateAbonnementCommand("gest-123", "InvalidPlan");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));

            _abonnementRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Abonnement>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _billingCacheMock.Verify(x => x.SetPlanLimitsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}