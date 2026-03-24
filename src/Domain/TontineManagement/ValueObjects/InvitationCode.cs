namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class InvitationCode : ValueObject
{
    private const int CodeLength = 6;
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Value { get; }

    private InvitationCode(string value)
    {
        Value = value;
    }

    public static InvitationCode Generate()
    {
        var random = Random.Shared;
        var code = string.Create(CodeLength, random, (span, rng) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = AllowedChars[rng.Next(AllowedChars.Length)];
        });

        return new InvitationCode(code);
    }

    public static InvitationCode From(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Invitation code must not be empty.", nameof(code));

        if (code.Length != CodeLength)
            throw new ArgumentException($"Invitation code must be exactly {CodeLength} characters.", nameof(code));

        var normalized = code.ToUpperInvariant();

        if (!normalized.All(AllowedChars.Contains))
            throw new ArgumentException("Invitation code must contain only alphanumeric characters.", nameof(code));

        return new InvitationCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
