namespace Domain.NotificationManagement.ValueObjects;

using Domain.Common;

/// <summary>
/// Value object encapsulating SMS message content.
/// Enforces max 160 characters to avoid concatenated SMS (cost x2).
/// </summary>
public sealed class ContenuMessage : ValueObject
{
    public const int MaxLength = 160;

    public string Texte { get; }

    private ContenuMessage(string texte)
    {
        Texte = texte;
    }

    public static ContenuMessage Create(string texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
            throw new ArgumentException("Le contenu du message ne peut pas être vide.", nameof(texte));

        if (texte.Length > MaxLength)
            throw new ArgumentException(
                $"Le contenu du message ne doit pas dépasser {MaxLength} caractères (actuellement {texte.Length}).",
                nameof(texte));

        return new ContenuMessage(texte);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Texte;
    }

    public override string ToString() => Texte;
}
