namespace AuthHandlerTests;

using Application.IdentityManagement.Commands.RegisterUtilisateur;
using Application.IdentityManagement.Services;
using Domain.Common;
using Domain.IdentityManagement;
using Domain.IdentityManagement.Repositories;
using Moq;

public class RegisterUtilisateurCommandHandlerTests
{
    private readonly Mock<IUtilisateurRepository> _utilisateurRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    private RegisterUtilisateurCommandHandler CreateHandler() =>
        new(
            _utilisateurRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object);

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNewUserId()
    {
        // Arrange
        var command = new RegisterUtilisateurCommand("+22670000000", "Moussa Diop", "SecurePass1");

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Utilisateur?)null);

        _passwordHasherMock
            .Setup(h => h.Hash("SecurePass1"))
            .Returns("$2a$11$hashed");

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Utilisateur>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingTelephone_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new RegisterUtilisateurCommand("+22670000000", "Moussa Diop", "SecurePass1");

        // We just need a dummy existing user
        var existingUser = Utilisateur.Create("+22670000000", "Existing User", "hash");

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("A user with telephone +22670000000 already exists.", ex.Message);

        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Utilisateur>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HashesPassword()
    {
        // Arrange
        var command = new RegisterUtilisateurCommand("+22670000000", "Test", "MyPassword1");

        _utilisateurRepositoryMock
            .Setup(r => r.GetByTelephoneAsync("+22670000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Utilisateur?)null);

        _passwordHasherMock
            .Setup(h => h.Hash("MyPassword1"))
            .Returns("$2a$11$hashedpassword");

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(h => h.Hash("MyPassword1"), Times.Once);

        // We can also verify that the hash is passed to the AddAsync
        _utilisateurRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Utilisateur>(u => u.MotDePasseHash.Value == "$2a$11$hashedpassword"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
