using Domain.IdentityManagement;
using Domain.IdentityManagement.Events;
using Domain.IdentityManagement.ValueObjects;

namespace DomainUnitsTest;

public class UtilisateurTests
{
    private static Utilisateur CreateDefaultUtilisateur(
        string telephone = "+22670001234",
        string nom = "Moussa Diop",
        string motDePasseHash = "hashedpassword123",
        RoleUtilisateur role = RoleUtilisateur.Membre)
    {
        return Utilisateur.Create(telephone, nom, motDePasseHash, role);
    }

    [Fact]
    public void Create_WithValidParameters_Succeeds()
    {
        var utilisateur = CreateDefaultUtilisateur();

        Assert.Equal("+22670001234", utilisateur.Telephone);
        Assert.Equal("Moussa Diop", utilisateur.Nom);
        Assert.Equal(RoleUtilisateur.Membre, utilisateur.Role);
        Assert.True(utilisateur.EstActif);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTelephone_ThrowsArgumentException(string? telephone)
    {
        Assert.Throws<ArgumentException>(() =>
            Utilisateur.Create(telephone!, "Nom", "hash"));
    }

    [Fact]
    public void Create_WithInvalidTelephone_NoPlusPrefix_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Utilisateur.Create("221770001234", "Nom", "hash"));
    }

    [Fact]
    public void Create_RaisesUtilisateurCreatedEvent()
    {
        var utilisateur = CreateDefaultUtilisateur();

        var domainEvent = Assert.Single(utilisateur.DomainEvents);
        var createdEvent = Assert.IsType<UtilisateurCreatedEvent>(domainEvent);
        Assert.Equal("+22670001234", createdEvent.Telephone);
        Assert.Equal("Moussa Diop", createdEvent.Nom);
    }

    [Fact]
    public void Desactiver_SetsEstActifFalse()
    {
        var utilisateur = CreateDefaultUtilisateur();

        utilisateur.Desactiver();

        Assert.False(utilisateur.EstActif);
    }

    [Fact]
    public void Activer_SetsEstActifTrue()
    {
        var utilisateur = CreateDefaultUtilisateur();
        utilisateur.Desactiver();

        utilisateur.Activer();

        Assert.True(utilisateur.EstActif);
    }

    [Fact]
    public void ChangerRole_UpdatesRole()
    {
        var utilisateur = CreateDefaultUtilisateur();

        utilisateur.ChangerRole(RoleUtilisateur.Admin);

        Assert.Equal(RoleUtilisateur.Admin, utilisateur.Role);
    }
}
