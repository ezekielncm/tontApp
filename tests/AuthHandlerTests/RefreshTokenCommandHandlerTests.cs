namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.RefreshToken;
using Application.IdentityManagement.Services;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Domain.IdentityManagement.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUtilisateurRepository> _utilisateurRepositoryMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock = new();

    private RefreshTokenCommandHandler CreateHandler() =>
        new(
            _utilisateurRepositoryMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand("valid-refresh-token");
        var utilisateur = Utilisateur.Create("+22670000000", "Test", "$2a$11$hash");

        _refreshTokenServiceMock
            .Setup(r => r.ValidateAndRotateAsync("valid-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UtilisateurId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(utilisateur))
            .Returns("new-access-token");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-refresh-token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_Throws()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-token");

        _refreshTokenServiceMock
            .Setup(r => r.ValidateAndRotateAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("invalide", ex.Message);
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand("valid-token");
        var utilisateur = Utilisateur.Create("+22670000000", "Test", "$2a$11$hash");
        utilisateur.Desactiver();

        _refreshTokenServiceMock
            .Setup(r => r.ValidateAndRotateAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UtilisateurId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("désactivé", ex.Message);
    }

    [Fact]
    public async Task Handle_RotatesToken_OldTokenInvalidated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshTokenCommand("old-refresh-token");
        var utilisateur = Utilisateur.Create("+22670000000", "Test", "$2a$11$hash");

        _refreshTokenServiceMock
            .Setup(r => r.ValidateAndRotateAsync("old-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _utilisateurRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UtilisateurId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilisateur);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(utilisateur))
            .Returns("new-token");

        _refreshTokenServiceMock
            .Setup(r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-refresh-token");

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — ValidateAndRotateAsync should be called (which invalidates old token)
        _refreshTokenServiceMock.Verify(
            r => r.ValidateAndRotateAsync("old-refresh-token", It.IsAny<CancellationToken>()), Times.Once);

        // New token should be generated
        _refreshTokenServiceMock.Verify(
            r => r.GenerateAndStoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
