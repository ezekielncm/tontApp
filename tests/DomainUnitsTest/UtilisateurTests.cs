using Domain.IdentityManagement;
using Domain.IdentityManagement.Events;
using Domain.IdentityManagement.ValueObjects;

namespace DomainUnitsTest;

public class UtilisateurTests
{
    private static Utilisateur CreateDefaultUtilisateur(
        string telephone = "+22670001234",
        string nom = "Moussa Diop",
        string motDePasseHash = "$2a$11$fakehashfortest000000000000000000000000000000",
        RoleUtilisateur role = RoleUtilisateur.Membre)
    {
        return Utilisateur.Create(telephone, nom, motDePasseHash, role);
    }

    [Fact]
    public void Create_WithValidParameters_Succeeds()
    {
        var utilisateur = CreateDefaultUtilisateur();

        Assert.Equal("+22670001234", utilisateur.Telephone.Value);
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
            Utilisateur.Create(telephone!, "Nom", "$2a$11$fakehash000000000000000000000000000000000000"));
    }

    [Fact]
    public void Create_WithInvalidTelephone_NoPlusPrefix_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Utilisateur.Create("221770001234", "Nom", "$2a$11$fakehash000000000000000000000000000000000000"));
    }

    [Fact]
    public void Create_RaisesUtilisateurInscritEvent()
    {
        var utilisateur = CreateDefaultUtilisateur();

        var domainEvent = Assert.Single(utilisateur.DomainEvents);
        var inscritEvent = Assert.IsType<UtilisateurInscritEvent>(domainEvent);
        Assert.Equal("+22670001234", inscritEvent.Telephone);
        Assert.Equal("Moussa Diop", inscritEvent.Nom);
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

    [Fact]
    public void Create_NormalizesE164Telephone()
    {
        var utilisateur = Utilisateur.Create("+226 70 00 12 34", "Test", "$2a$11$fakehash000000000000000000000000000000000000");

        Assert.Equal("+22670001234", utilisateur.Telephone.Value);
    }

    [Fact]
    public void TelephoneId_Create_WithValidE164_Succeeds()
    {
        var tel = TelephoneId.Create("+22670000000");
        Assert.Equal("+22670000000", tel.Value);
    }

    [Fact]
    public void TelephoneId_Create_WithInvalidFormat_Throws()
    {
        Assert.Throws<ArgumentException>(() => TelephoneId.Create("0670000000"));
    }

    [Fact]
    public void MotDePasseHash_ToString_ReturnsRedacted()
    {
        var hash = MotDePasseHash.FromHash("$2a$11$somehash");
        Assert.Equal("***REDACTED***", hash.ToString());
    }

    [Fact]
    public void MotDePasseHash_FromHash_WithEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => MotDePasseHash.FromHash(""));
    }
}
