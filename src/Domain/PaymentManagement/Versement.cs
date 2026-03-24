namespace Domain.PaymentManagement;

using System.Text.Json;
using Domain.Common;
using Domain.PaymentManagement.Entities;
using Domain.PaymentManagement.Events;
using Domain.PaymentManagement.ValueObjects;
using Domain.TontineManagement.ValueObjects;

public class Versement : AggregateRoot<VersementId>
{
    private readonly List<AuditEntry> _auditTrail = [];

    public TontineId TontineId { get; private set; }
    public MemberId MemberId { get; private set; }
    public RoundId RoundId { get; private set; }
    public decimal Montant { get; private set; }
    public string Currency { get; private set; }
    public VersementStatus Statut { get; private set; }
    public string? ReferenceExterne { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public IReadOnlyCollection<AuditEntry> AuditTrail => _auditTrail.AsReadOnly();

    private Versement() : base()
    {
        TontineId = default!;
        MemberId = default!;
        RoundId = default!;
        Currency = string.Empty;
    }

    private Versement(
        VersementId id,
        TontineId tontineId,
        MemberId memberId,
        RoundId roundId,
        decimal montant,
        string currency) : base(id)
    {
        TontineId = tontineId;
        MemberId = memberId;
        RoundId = roundId;
        Montant = montant;
        Currency = currency;
        Statut = VersementStatus.EnAttente;
        CreatedAt = DateTime.UtcNow;
    }

    public static Versement Create(
        TontineId tontineId,
        MemberId memberId,
        RoundId roundId,
        decimal montant,
        string currency)
    {
        if (montant <= 0)
            throw new ArgumentException("Montant must be greater than zero.", nameof(montant));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must not be empty.", nameof(currency));

        var versement = new Versement(
            VersementId.Create(),
            tontineId,
            memberId,
            roundId,
            montant,
            currency);

        var payload = JsonSerializer.Serialize(new
        {
            versement.TontineId.Value,
            MemberId = versement.MemberId.Value,
            RoundId = versement.RoundId.Value,
            versement.Montant,
            versement.Currency
        });

        versement.AddAuditEntry("system", "VersementCree", payload);

        versement.AddDomainEvent(new VersementCreatedEvent(
            versement.Id,
            tontineId,
            memberId,
            montant));

        return versement;
    }

    public void Confirmer(string referenceExterne)
    {
        if (Statut != VersementStatus.EnAttente)
            throw new InvalidOperationException("Only a pending versement can be confirmed.");

        Statut = VersementStatus.Confirme;
        ConfirmedAt = DateTime.UtcNow;
        ReferenceExterne = referenceExterne;

        var payload = JsonSerializer.Serialize(new { ReferenceExterne = referenceExterne });
        AddAuditEntry("system", "VersementConfirme", payload);

        AddDomainEvent(new VersementConfirmedEvent(
            Id,
            TontineId,
            MemberId,
            RoundId,
            Montant,
            referenceExterne));
    }

    public void Echouer(string raison)
    {
        if (Statut != VersementStatus.EnAttente)
            throw new InvalidOperationException("Only a pending versement can be marked as failed.");

        Statut = VersementStatus.Echoue;

        var payload = JsonSerializer.Serialize(new { Raison = raison });
        AddAuditEntry("system", "VersementEchoue", payload);
    }

    public bool VerifierIntegrite()
    {
        var previousHash = string.Empty;

        foreach (var entry in _auditTrail)
        {
            if (!entry.VerifyIntegrity(previousHash))
                return false;

            previousHash = entry.Hash;
        }

        return true;
    }

    private void AddAuditEntry(string actorId, string action, string payload)
    {
        var previousHash = _auditTrail.Count > 0
            ? _auditTrail[^1].Hash
            : string.Empty;

        var entry = AuditEntry.Create(previousHash, actorId, action, payload);
        _auditTrail.Add(entry);
    }
}
