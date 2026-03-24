namespace Domain.TontineManagement.Entities;

using Domain.Common;
using Domain.TontineManagement.ValueObjects;

public class Invitation : Entity<InvitationId>
{
    public InvitationCode Code { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsMultipleUse { get; private set; }

    private Invitation() : base()
    {
        Code = default!;
    }

    internal Invitation(InvitationId id, InvitationCode code, DateTime expiresAt, bool isMultipleUse) : base(id)
    {
        Code = code;
        ExpiresAt = expiresAt;
        IsUsed = false;
        IsMultipleUse = isMultipleUse;
    }

    public static Invitation Create(bool isMultipleUse = false)
    {
        return new Invitation(
            InvitationId.Create(),
            InvitationCode.Generate(),
            DateTime.UtcNow.AddDays(7),
            isMultipleUse);
    }

    public void MarkUsed()
    {
        if (!IsMultipleUse)
            IsUsed = true;
    }

    public bool IsValid()
    {
        if (IsMultipleUse)
            return ExpiresAt > DateTime.UtcNow;

        return !IsUsed && ExpiresAt > DateTime.UtcNow;
    }
}
