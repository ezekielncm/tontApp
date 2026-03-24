namespace Domain.PaymentManagement.Entities;

using System.Security.Cryptography;
using System.Text;
using Domain.Common;
using Domain.PaymentManagement.ValueObjects;

public class AuditEntry : Entity<AuditEntryId>
{
    public string PreviousHash { get; private set; }
    public string Hash { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string ActorId { get; private set; }
    public string Action { get; private set; }
    public string Payload { get; private set; }

    private AuditEntry() : base()
    {
        PreviousHash = string.Empty;
        Hash = string.Empty;
        ActorId = string.Empty;
        Action = string.Empty;
        Payload = string.Empty;
    }

    internal AuditEntry(AuditEntryId id, string previousHash, string hash, DateTime timestamp, string actorId, string action, string payload)
        : base(id)
    {
        PreviousHash = previousHash;
        Hash = hash;
        Timestamp = timestamp;
        ActorId = actorId;
        Action = action;
        Payload = payload;
    }

    public static AuditEntry Create(string previousHash, string actorId, string action, string payload)
    {
        var id = AuditEntryId.Create();
        var timestamp = DateTime.UtcNow;
        var hash = ComputeHash(previousHash, timestamp, actorId, action, payload);

        return new AuditEntry(id, previousHash, hash, timestamp, actorId, action, payload);
    }

    public bool VerifyIntegrity(string expectedPreviousHash)
    {
        var recomputedHash = ComputeHash(expectedPreviousHash, Timestamp, ActorId, Action, Payload);
        return Hash == recomputedHash && PreviousHash == expectedPreviousHash;
    }

    private static string ComputeHash(string previousHash, DateTime timestamp, string actorId, string action, string payload)
    {
        var input = $"{previousHash}{timestamp:O}{actorId}{action}{payload}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
