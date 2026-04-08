namespace Domain.PaymentManagement.Entities;

using System.Security.Cryptography;
using System.Text;
using Domain.Common;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public class AuditEntry : Entity<AuditEntryId>
{
    /// <summary>
    /// SHA-256 hash of "GENESIS_TONTINESAPP" — used as hashPrecedent for the first entry in a tontine chain.
    /// </summary>
    public static readonly string GenesisHash = ComputeSha256("GENESIS_TONTINESAPP");

    public VersementId VersementId { get; private set; }
    public TontineId TontineId { get; private set; }
    public AuditAction Action { get; private set; }
    public string ActeurId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Payload { get; private set; }
    public string HashPrecedent { get; private set; }
    public string HashCourant { get; private set; }

    private AuditEntry() : base()
    {
        VersementId = default!;
        TontineId = default!;
        ActeurId = string.Empty;
        Payload = string.Empty;
        HashPrecedent = string.Empty;
        HashCourant = string.Empty;
    }

    internal AuditEntry(
        AuditEntryId id,
        TontineId tontineId,
        VersementId versementId,
        AuditAction action,
        string acteurId,
        DateTime timestamp,
        string payload,
        string hashPrecedent,
        string hashCourant)
        : base(id)
    {
        TontineId = tontineId;
        VersementId = versementId;
        Action = action;
        ActeurId = acteurId;
        Timestamp = timestamp;
        Payload = payload;
        HashPrecedent = hashPrecedent;
        HashCourant = hashCourant;
    }

    /// <summary>
    /// Creates a new AuditEntry with a computed hash chain.
    /// Hash is computed as SHA-256 of: id|action|acteurId|timestamp|payload|hashPrecedent
    /// </summary>
    public static AuditEntry Create(
        TontineId tontineId,
        VersementId versementId,
        AuditAction action,
        string acteurId,
        string payload,
        string hashPrecedent)
    {
        var id = AuditEntryId.Create();
        var timestamp = DateTime.UtcNow;
        var hashCourant = ComputeHash(id, action, acteurId, timestamp, payload, hashPrecedent);

        return new AuditEntry(id, tontineId, versementId, action, acteurId, timestamp, payload, hashPrecedent, hashCourant);
    }

    /// <summary>
    /// Verifies this entry's integrity by recomputing the hash and checking the chain link.
    /// </summary>
    public bool VerifyIntegrity(string expectedHashPrecedent)
    {
        var recomputedHash = ComputeHash(Id, Action, ActeurId, Timestamp, Payload, expectedHashPrecedent);
        return HashCourant == recomputedHash && HashPrecedent == expectedHashPrecedent;
    }

    /// <summary>
    /// Computes the SHA-256 hash over: id|action|acteurId|timestamp|payload|hashPrecedent
    /// </summary>
    private static string ComputeHash(
        AuditEntryId id,
        AuditAction action,
        string acteurId,
        DateTime timestamp,
        string payload,
        string hashPrecedent)
    {
        var input = $"{id.Value}|{action}|{acteurId}|{timestamp:O}|{payload}|{hashPrecedent}";
        return ComputeSha256(input);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
