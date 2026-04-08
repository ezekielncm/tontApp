namespace Domain.PaymentManagement.ValueObjects;

using Domain.Common;

public sealed class TourId : ValueObject
{
    public Guid Value { get; }

    private TourId(Guid value)
    {
        Value = value;
    }

    public static TourId Create() => new(Guid.NewGuid());

    public static TourId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TourId cannot be empty.", nameof(value));

        return new TourId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
