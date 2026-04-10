namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.InscrireUtilisateur;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.NotificationManagement.Ports;
using Microsoft.Extensions.Logging;
using Moq;

public class InscrireUtilisateurCommandHandlerTests
{
    private readonly Mock<IUtilisateurRepository> _utilisateurRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<ISmsGateway> _smsGatewayMock = new();
    private readonly Mock<ILogger<InscrireUtilisateurCommandHandler>> _loggerMock = new();

    private InscrireUtilisateurCommandHandler CreateHandler() =>
        new(
            _utilisateurRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _otpServiceMock.Object,
            _smsGatewayMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResultWithOtpMessage()
    {
        // Arrange
        var command = new InscrireUtilisateurCommand("+22670000000", "Moussa Diop", "SecurePass1");

        _utilisateurRepositoryMock
            .Setup(r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash("SecurePass1"))
            .Returns("$2a$11$hashed");

        _otpServiceMock
            .Setup(o => o.GenerateAndStoreAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        _smsGatewayMock
            .Setup(s => s.EnvoyerAsync("+22670000000", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(true, "msg-1", null));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.UtilisateurId);
        Assert.Contains("OTP", result.Message);

        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Utilisateur>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _smsGatewayMock.Verify(
            s => s.EnvoyerAsync("+22670000000", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingTelephone_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new InscrireUtilisateurCommand("+22670000000", "Moussa Diop", "SecurePass1");

        _utilisateurRepositoryMock
            .Setup(r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Utilisateur>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HashesPasswordWithBCrypt()
    {
        // Arrange
        var command = new InscrireUtilisateurCommand("+22670000000", "Test", "MyPassword1");

        _utilisateurRepositoryMock
            .Setup(r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash("MyPassword1"))
            .Returns("$2a$11$hashedpassword");

        _otpServiceMock
            .Setup(o => o.GenerateAndStoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("654321");

        _smsGatewayMock
            .Setup(s => s.EnvoyerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(true, "msg-2", null));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(h => h.Hash("MyPassword1"), Times.Once);
    }
}
