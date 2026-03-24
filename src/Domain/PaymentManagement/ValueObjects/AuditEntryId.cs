namespace Domain.PaymentManagement.ValueObjects;

using Domain.Common;

public sealed class AuditEntryId : ValueObject
{
    public Guid Value { get; }

    private AuditEntryId(Guid value)
    {
        Value = value;
    }

    public static AuditEntryId Create() => new(Guid.NewGuid());

    public static AuditEntryId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AuditEntryId cannot be empty.", nameof(value));

        return new AuditEntryId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
