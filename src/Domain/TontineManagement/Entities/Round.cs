namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public class Round : Entity<RoundId>
{
    public int RoundNumber { get; private set; }
    public MemberId BeneficiaryId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public bool IsCompleted { get; private set; }

    private Round() : base()
    {
        BeneficiaryId = default!;
    }

    internal Round(RoundId id, int roundNumber, MemberId beneficiaryId, DateTime scheduledDate) : base(id)
    {
        RoundNumber = roundNumber;
        BeneficiaryId = beneficiaryId;
        ScheduledDate = scheduledDate;
        IsCompleted = false;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
