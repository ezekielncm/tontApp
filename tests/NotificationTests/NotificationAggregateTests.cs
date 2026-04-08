using Domain.NotificationManagement;
using Domain.NotificationManagement.ValueObjects;

namespace NotificationTests;

public class NotificationAggregateTests
{
    [Fact]
    public void CreateFull_WithSmsCanal_ValidatesE164Format()
    {
        // Valid E.164
        var notification = Notification.CreateFull(
            "+22670000000",
            Canal.SMS,
            NotificationType.RappelPaiement,
            "Test rappel");

        Assert.Equal(Canal.SMS, notification.Canal);
        Assert.Equal("+22670000000", notification.DestinataireId);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("invalid")]
    [InlineData("+0")]
    public void CreateFull_WithInvalidE164_ThrowsForSms(string phone)
    {
        Assert.Throws<ArgumentException>(() =>
            Notification.CreateFull(phone, Canal.SMS, NotificationType.Bienvenue, "Test"));
    }

    [Fact]
    public void CreateFull_WithPushCanal_DoesNotValidateE164()
    {
        // PUSH canal doesn't require E.164
        var notification = Notification.CreateFull(
            "user-123",
            Canal.PUSH,
            NotificationType.Bienvenue,
            "Bienvenue!");

        Assert.Equal(Canal.PUSH, notification.Canal);
    }

    [Fact]
    public void CreateFull_SetsContenuMessage()
    {
        var notification = Notification.CreateFull(
            "+22670000000",
            Canal.SMS,
            NotificationType.ConfirmationPaiement,
            "Paiement recu. Merci!");

        Assert.NotNull(notification.ContenuMessage);
        Assert.Equal("Paiement recu. Merci!", notification.ContenuMessage.Texte);
        Assert.Equal("Paiement recu. Merci!", notification.Contenu);
    }

    [Fact]
    public void CreateFull_WithDateEnvoi_SetsScheduledDate()
    {
        var scheduledDate = DateTime.UtcNow.AddHours(2);

        var notification = Notification.CreateFull(
            "+22670000000",
            Canal.SMS,
            NotificationType.RappelPaiement,
            "Rappel",
            dateEnvoi: scheduledDate);

        Assert.Equal(scheduledDate, notification.DateEnvoi);
    }

    [Fact]
    public void Create_BackwardsCompatible_DefaultsToSms()
    {
        var notification = Notification.Create(
            "user-123",
            NotificationType.Bienvenue,
            "Bienvenue dans la tontine!");

        Assert.Equal(Canal.SMS, notification.Canal);
        Assert.NotNull(notification.ContenuMessage);
    }

    [Fact]
    public void EstCritique_ReturnsTrueForConfirmationPaiement()
    {
        var notification = Notification.CreateFull(
            "+22670000000",
            Canal.SMS,
            NotificationType.ConfirmationPaiement,
            "Paiement de 5000 XOF recu");

        Assert.True(notification.EstCritique());
    }

    [Theory]
    [InlineData(NotificationType.RappelPaiement)]
    [InlineData(NotificationType.OuvertureTour)]
    [InlineData(NotificationType.RecapHebdomadaire)]
    [InlineData(NotificationType.Bienvenue)]
    public void EstCritique_ReturnsFalseForNonCriticalTypes(NotificationType type)
    {
        var notification = Notification.CreateFull(
            "+22670000000",
            Canal.SMS,
            type,
            "Test message");

        Assert.False(notification.EstCritique());
    }

    [Fact]
    public void CreateFull_WithLongMessage_ThrowsArgumentException()
    {
        var longMessage = new string('X', 161);

        Assert.Throws<ArgumentException>(() =>
            Notification.CreateFull(
                "+22670000000",
                Canal.SMS,
                NotificationType.Bienvenue,
                longMessage));
    }
}
