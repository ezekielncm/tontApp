namespace Domain.TontineManagement.Events;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public sealed class MembreSuspenduEvent : IDomainEvent
{
    public TontineId TontineId { get; }
    public MemberId MembreId { get; }
    public string Motif { get; }
    public DateTime OccurredOn { get; }

    public MembreSuspenduEvent(TontineId tontineId, MemberId membreId, string motif)
    {
        TontineId = tontineId;
        MembreId = membreId;
        Motif = motif;
        OccurredOn = DateTime.UtcNow;
    }
}
