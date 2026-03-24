namespace Domain.PaymentManagement.ValueObjects;

using Domain.Common;

public sealed class VersementId : ValueObject
{
    public Guid Value { get; }

    private VersementId(Guid value)
    {
        Value = value;
    }

    public static VersementId Create() => new(Guid.NewGuid());

    public static VersementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VersementId cannot be empty.", nameof(value));

        return new VersementId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
