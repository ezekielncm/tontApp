using Domain.NotificationManagement.ValueObjects;

namespace NotificationTests;

public class ContenuMessageTests
{
    [Fact]
    public void Create_WithValidText_Succeeds()
    {
        var contenu = ContenuMessage.Create("Test message");

        Assert.Equal("Test message", contenu.Texte);
    }

    [Fact]
    public void Create_WithExactly160Chars_Succeeds()
    {
        var text = new string('A', 160);
        var contenu = ContenuMessage.Create(text);

        Assert.Equal(160, contenu.Texte.Length);
    }

    [Fact]
    public void Create_With161Chars_ThrowsArgumentException()
    {
        var text = new string('A', 161);

        var ex = Assert.Throws<ArgumentException>(() => ContenuMessage.Create(text));
        Assert.Contains("160", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyText_ThrowsArgumentException(string? text)
    {
        Assert.Throws<ArgumentException>(() => ContenuMessage.Create(text!));
    }

    [Fact]
    public void ToString_ReturnsTexte()
    {
        var contenu = ContenuMessage.Create("Hello World");

        Assert.Equal("Hello World", contenu.ToString());
    }

    [Fact]
    public void Equality_SameTexte_AreEqual()
    {
        var a = ContenuMessage.Create("Same message");
        var b = ContenuMessage.Create("Same message");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentTexte_AreNotEqual()
    {
        var a = ContenuMessage.Create("Message A");
        var b = ContenuMessage.Create("Message B");

        Assert.NotEqual(a, b);
    }
}
