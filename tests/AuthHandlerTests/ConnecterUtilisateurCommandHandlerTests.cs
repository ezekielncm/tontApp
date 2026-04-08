namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.ConnecterUtilisateur;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

public class ConnecterUtilisateurCommandHandlerTests
{
    private readonly Mock<IUtilisateurRepository> _utilisateurRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILoginAttemptService> _loginAttemptServiceMock = new();
    private readonly Mock<ILogger<ConnecterUtilisateurCommandHandler>> _loggerMock = new();

    private ConnecterUtilisateurCommandHandler CreateHandler() =>
        new(
            _utilisateurRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _loginAttemptServiceMock.Object,
            _loggerMock.Object);

    private static Utilisateur CreateTestUtilisateur(
        string telephone = "+22670000000",
        string nom = "Test User",
        string hash = "$2a$11$validhash")
    {
        return Utilisateur.Create(telephone, nom, hash);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResult()
    {
        // Arrange
        var command = new ConnecterUtilisateurCommand("+22670000000", "SecurePass1");
        var utilisateur = CreateTestUtilisateur();

        _loginAttemptServiceMock
            .Setup(l => l.IsLockedOutAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        _passwordHasherMock
            .Setup(h => h.Verify("SecurePass1", "$2a$11$validhash"))
            .Returns(true);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(utilisateur))
            .Returns("access-token");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        _loginAttemptServiceMock.Verify(
            l => l.ResetAttemptsAsync("+22670000000", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ThrowsAndRegistersFailedAttempt()
    {
        // Arrange
        var command = new ConnecterUtilisateurCommand("+22670000000", "WrongPassword");
        var utilisateur = CreateTestUtilisateur();

        _loginAttemptServiceMock
            .Setup(l => l.IsLockedOutAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        _passwordHasherMock
            .Setup(h => h.Verify("WrongPassword", "$2a$11$validhash"))
            .Returns(false);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        _loginAttemptServiceMock.Verify(
            l => l.RegisterFailedAttemptAsync("+22670000000", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ThrowsWithoutCheckingPassword()
    {
        // Arrange
        var command = new ConnecterUtilisateurCommand("+22670000000", "AnyPass");

        _loginAttemptServiceMock
            .Setup(l => l.IsLockedOutAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("verrouillé", ex.Message);

        _utilisateurRepositoryMock.Verify(
            r => r.GetByTelephoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownTelephone_ThrowsAndRegistersFailedAttempt()
    {
        // Arrange
        var command = new ConnecterUtilisateurCommand("+22670000000", "AnyPass");

        _loginAttemptServiceMock
            .Setup(l => l.IsLockedOutAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Utilisateur?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        _loginAttemptServiceMock.Verify(
            l => l.RegisterFailedAttemptAsync("+22670000000", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDeactivatedAccount_Throws()
    {
        // Arrange
        var command = new ConnecterUtilisateurCommand("+22670000000", "SecurePass1");
        var utilisateur = CreateTestUtilisateur();
        utilisateur.Desactiver();

        _loginAttemptServiceMock
            .Setup(l => l.IsLockedOutAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("désactivé", ex.Message);
    }
}
