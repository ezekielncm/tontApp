namespace Domain.TontineManagement.ValueObjects;

using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// Generates a cryptographically secure 6-character alphanumeric code.
    /// Uses System.Security.Cryptography.RandomNumberGenerator instead of Math.Random.
    /// </summary>
    public static InvitationCode Generate()
    {
        Span<byte> randomBytes = stackalloc byte[CodeLength];
        RandomNumberGenerator.Fill(randomBytes);

        var code = string.Create(CodeLength, randomBytes.ToArray(), static (span, bytes) =>
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (int i = 0; i < span.Length; i++)
                span[i] = chars[bytes[i] % chars.Length];
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

    /// <summary>
    /// Computes a SHA256 hash of the given plain-text invitation code.
    /// Used to store codes hashed in the database (never in plain text).
    /// </summary>
    public static string ComputeHash(string plainCode)
    {
        var normalized = plainCode.ToUpperInvariant();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(hashBytes);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
