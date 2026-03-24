using Domain.NotificationManagement;
using Domain.NotificationManagement.Events;
using Domain.NotificationManagement.ValueObjects;

namespace DomainUnitsTest;

public class NotificationTests
{
    private static Notification CreateDefaultNotification(
        string destinataireId = "user-123",
        NotificationType type = NotificationType.RappelPaiement,
        string contenu = "Test notification",
        int maxTentatives = 3)
    {
        return Notification.Create(destinataireId, type, contenu, maxTentatives);
    }

    [Fact]
    public void Create_WithValidParameters_SetsStatusEnAttente()
    {
        var notification = CreateDefaultNotification();

        Assert.Equal(NotificationStatus.EnAttente, notification.Statut);
        Assert.Equal("user-123", notification.DestinataireId);
        Assert.Equal(NotificationType.RappelPaiement, notification.Type);
        Assert.Equal("Test notification", notification.Contenu);
        Assert.Equal(0, notification.TentativesEnvoi);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyDestinataire_ThrowsArgumentException(string? destinataireId)
    {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(destinataireId!, NotificationType.Bienvenue, "content"));
    }

    [Fact]
    public void Create_RaisesNotificationCreatedEvent()
    {
        var notification = CreateDefaultNotification();

        var domainEvent = Assert.Single(notification.DomainEvents);
        Assert.IsType<NotificationCreatedEvent>(domainEvent);
    }

    [Fact]
    public void MarquerEnvoyee_SetsEnvoyeeStatus()
    {
        var notification = CreateDefaultNotification();

        notification.MarquerEnvoyee();

        Assert.Equal(NotificationStatus.Envoyee, notification.Statut);
        Assert.NotNull(notification.SentAt);
    }

    [Fact]
    public void MarquerEchouee_IncrementsTentatives()
    {
        var notification = CreateDefaultNotification(maxTentatives: 3);

        notification.MarquerEchouee();

        Assert.Equal(1, notification.TentativesEnvoi);
        Assert.Equal(NotificationStatus.EnAttente, notification.Statut);
    }

    [Fact]
    public void MarquerEchouee_SetsEchoueeAfterMaxRetries()
    {
        var notification = CreateDefaultNotification(maxTentatives: 2);

        notification.MarquerEchouee(); // 1
        var result = notification.MarquerEchouee(); // 2 = max

        Assert.False(result);
        Assert.Equal(NotificationStatus.Echouee, notification.Statut);
    }

    [Fact]
    public void MarquerEchouee_ReturnsTrueWhenRetriesAvailable()
    {
        var notification = CreateDefaultNotification(maxTentatives: 3);

        var result = notification.MarquerEchouee();

        Assert.True(result);
    }

    [Fact]
    public void PeutReessayer_ReturnsTrue_WhenRetriesAvailable()
    {
        var notification = CreateDefaultNotification(maxTentatives: 3);

        Assert.True(notification.PeutReessayer());
    }

    [Fact]
    public void PeutReessayer_ReturnsFalse_WhenEnvoyee()
    {
        var notification = CreateDefaultNotification();
        notification.MarquerEnvoyee();

        Assert.False(notification.PeutReessayer());
    }

    [Fact]
    public void PeutReessayer_ReturnsFalse_WhenMaxRetriesReached()
    {
        var notification = CreateDefaultNotification(maxTentatives: 1);
        notification.MarquerEchouee();

        Assert.False(notification.PeutReessayer());
    }

    [Fact]
    public void MarquerEchouee_WhenAlreadySent_ThrowsInvalidOperationException()
    {
        var notification = CreateDefaultNotification();
        notification.MarquerEnvoyee();

        Assert.Throws<InvalidOperationException>(() => notification.MarquerEchouee());
    }
}
