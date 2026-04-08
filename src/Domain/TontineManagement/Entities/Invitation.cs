namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public class Invitation : Entity<InvitationId>
{
    /// <summary>
    /// SHA256 hash of the plain-text invitation code. Never store the code in plain text.
    /// </summary>
    public string CodeHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Maximum number of times this invitation code can be used (1 = single-use).
    /// </summary>
    public int NombreUsagesMax { get; private set; }

    /// <summary>
    /// Current number of times this invitation code has been used.
    /// </summary>
    public int NombreUsagesActuels { get; private set; }

    private Invitation() : base()
    {
        CodeHash = default!;
    }

    internal Invitation(InvitationId id, string codeHash, DateTime expiresAt, int nombreUsagesMax) : base(id)
    {
        CodeHash = codeHash;
        ExpiresAt = expiresAt;
        NombreUsagesMax = nombreUsagesMax;
        NombreUsagesActuels = 0;
    }

    /// <summary>
    /// Creates a new invitation. Returns the invitation (with hashed code) and the plain-text code
    /// that must be shared with the invitee. The plain code is NOT stored in the entity.
    /// </summary>
    public static (Invitation Invitation, string PlainCode) Create(int nombreUsagesMax = 1, int expirationDays = 7)
    {
        if (nombreUsagesMax < 1)
            throw new ArgumentException("Le nombre d'usages maximum doit être au moins 1.", nameof(nombreUsagesMax));

        if (expirationDays < 1)
            throw new ArgumentException("La durée d'expiration doit être d'au moins 1 jour.", nameof(expirationDays));

        var code = InvitationCode.Generate();
        var codeHash = InvitationCode.ComputeHash(code.Value);

        var invitation = new Invitation(
            InvitationId.Create(),
            codeHash,
            DateTime.UtcNow.AddDays(expirationDays),
            nombreUsagesMax);

        return (invitation, code.Value);
    }

    /// <summary>
    /// Increments the usage count. Throws if the invitation has reached its maximum usage count.
    /// </summary>
    public void IncrementUsage()
    {
        if (NombreUsagesActuels >= NombreUsagesMax)
            throw new InvalidOperationException("This invitation code has reached its maximum number of uses.");

        NombreUsagesActuels++;
    }

    /// <summary>
    /// Checks whether the code is still valid (not expired, usage count not exceeded).
    /// </summary>
    public bool IsValid()
    {
        return ExpiresAt > DateTime.UtcNow && NombreUsagesActuels < NombreUsagesMax;
    }

    /// <summary>
    /// Verifies whether the given plain-text code matches this invitation's hash.
    /// </summary>
    public bool MatchesCode(string plainCode)
    {
        var hash = InvitationCode.ComputeHash(plainCode);
        return string.Equals(CodeHash, hash, StringComparison.Ordinal);
    }
}
