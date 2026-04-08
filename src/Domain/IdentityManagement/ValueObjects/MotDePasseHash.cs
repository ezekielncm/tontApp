namespace Domain.IdentityManagement.ValueObjects;

using Domain.Common;

public sealed class MotDePasseHash : ValueObject
{
    public string Value { get; }

    private MotDePasseHash(string value)
    {
        Value = value;
    }

    public static MotDePasseHash FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash must not be empty.", nameof(hash));

        return new MotDePasseHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => "***REDACTED***";
}
