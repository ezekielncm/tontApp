using Domain.NotificationManagement.ValueObjects;

namespace NotificationTests;

public class SmsTemplateTests
{
    [Fact]
    public void TourOuvert_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.TourOuvert("Tontine Solidaire Burkina", 5);

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("Tour 5", message);
        Assert.Contains("Tontine Solidaire Burkina", message);
    }

    [Fact]
    public void VersementConfirme_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.VersementConfirme(50000, "XOF", "Tontine Solidaire Ouagadougou");

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("50000", message);
        Assert.Contains("XOF", message);
    }

    [Fact]
    public void RappelJ3_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.RappelJ3("Tontine du Quartier Nord", 10000, "XOF");

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("3 jours", message);
        Assert.Contains("10000", message);
    }

    [Fact]
    public void RappelJ1_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.RappelJ1("Tontine du Quartier Nord", 10000, "XOF");

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("demain", message);
    }

    [Fact]
    public void PaiementEnRetard_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.PaiementEnRetard("Ma Grande Tontine de Ouagadougou", 25000, "XOF");

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("retard", message);
    }

    [Fact]
    public void RecapHebdomadaire_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.RecapHebdomadaire("Tontine Famille Ouedraogo", 8, 10);

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("8/10", message);
    }

    [Fact]
    public void Bienvenue_ReturnsMessageUnder160Chars()
    {
        var message = SmsTemplate.Bienvenue("Tontine des Amis de Bobo-Dioulasso");

        Assert.True(message.Length <= ContenuMessage.MaxLength,
            $"Message length {message.Length} exceeds max {ContenuMessage.MaxLength}");
        Assert.Contains("Bienvenue", message);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Very Long Tontine Name That Could Possibly Exceed Character Limit With Full Details")]
    public void AllTemplates_NeverExceed160Chars(string longName)
    {
        var templates = new[]
        {
            SmsTemplate.TourOuvert(longName, 99),
            SmsTemplate.VersementConfirme(999999.99m, "XOF", longName),
            SmsTemplate.RappelJ3(longName, 999999.99m, "XOF"),
            SmsTemplate.RappelJ1(longName, 999999.99m, "XOF"),
            SmsTemplate.PaiementEnRetard(longName, 999999.99m, "XOF"),
            SmsTemplate.RecapHebdomadaire(longName, 999, 999),
            SmsTemplate.Bienvenue(longName)
        };

        foreach (var template in templates)
        {
            Assert.True(template.Length <= ContenuMessage.MaxLength,
                $"Template exceeds {ContenuMessage.MaxLength} chars: '{template}' ({template.Length} chars)");
        }
    }
}
