namespace Domain.IdentityManagement.ValueObjects;

using System.Text.RegularExpressions;
using Domain.Common;

public sealed partial class TelephoneId : ValueObject
{
    private static readonly Regex E164Regex = GenerateE164Regex();

    public string Value { get; }

    private TelephoneId(string value)
    {
        Value = value;
    }

    public static TelephoneId Create(string telephone)
    {
        if (string.IsNullOrWhiteSpace(telephone))
            throw new ArgumentException("Telephone must not be empty.", nameof(telephone));

        var normalized = Normalize(telephone);

        if (!E164Regex.IsMatch(normalized))
            throw new ArgumentException(
                "Telephone must be in E.164 format (e.g. +22670000000).", nameof(telephone));

        return new TelephoneId(normalized);
    }

    public static TelephoneId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Telephone must not be empty.", nameof(value));

        return new TelephoneId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    private static string Normalize(string telephone)
    {
        // Remove spaces, dashes, parentheses
        return telephone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
    }

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex GenerateE164Regex();
}
