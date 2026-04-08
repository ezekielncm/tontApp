namespace Domain.PaymentManagement.ValueObjects;

using Domain.Common;

public sealed class PayeurId : ValueObject
{
    public Guid Value { get; }

    private PayeurId(Guid value)
    {
        Value = value;
    }

    public static PayeurId Create() => new(Guid.NewGuid());

    public static PayeurId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PayeurId cannot be empty.", nameof(value));

        return new PayeurId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
