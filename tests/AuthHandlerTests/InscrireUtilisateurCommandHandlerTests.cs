namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.InscrireUtilisateur;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

public class InscrireUtilisateurCommandHandlerTests
{
    private readonly Mock<IUtilisateurRepository> _utilisateurRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<InscrireUtilisateurCommandHandler>> _loggerMock = new();

    private InscrireUtilisateurCommandHandler CreateHandler() =>
        new(
            _utilisateurRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsAuthResult()
    {
        // Arrange
        var command = new InscrireUtilisateurCommand("+22670000000", "Moussa Diop", "SecurePass1");

        _utilisateurRepositoryMock
            .Setup(r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash("SecurePass1"))
            .Returns("$2a$11$hashed");

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<Utilisateur>()))
            .Returns("access-token-123");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token-456");

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token-123", result.AccessToken);
        Assert.Equal("refresh-token-456", result.RefreshToken);
        Assert.NotEqual(Guid.Empty, result.UtilisateurId);

        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Utilisateur>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<Utilisateur>()))
            .Returns("token");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(h => h.Hash("MyPassword1"), Times.Once);
    }

    [Fact]
    public async Task Handle_NormalizesPhoneNumberE164()
    {
        // Arrange — phone with spaces should still work
        var command = new InscrireUtilisateurCommand("+226 70 00 00 00", "Test", "SecurePass1");

        _utilisateurRepositoryMock
            .Setup(r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("$2a$11$hash");

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<Utilisateur>()))
            .Returns("token");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — phone is normalized before checking existence
        _utilisateurRepositoryMock.Verify(
            r => r.ExistsByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()), Times.Once);
    }
}
