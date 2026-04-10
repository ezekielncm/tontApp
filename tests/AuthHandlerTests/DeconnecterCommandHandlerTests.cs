namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.Deconnecter;
using Application.IdentityManagement.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class DeconnecterCommandHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<IAccessTokenBlacklistService> _blacklistServiceMock = new();
    private readonly Mock<ILogger<DeconnecterCommandHandler>> _loggerMock = new();

    private DeconnecterCommandHandler CreateHandler() =>
        new(
            _refreshTokenServiceMock.Object,
            _blacklistServiceMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_RevokesRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeconnecterCommand(userId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            r => r.RevokeAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentUserId_RevokesCorrectToken()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeconnecterCommand(userId1), CancellationToken.None);
        await handler.Handle(new DeconnecterCommand(userId2), CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            r => r.RevokeAsync(userId1, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenServiceMock.Verify(
            r => r.RevokeAsync(userId2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
