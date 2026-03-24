namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class RoundId : ValueObject
{
    public Guid Value { get; }

    private RoundId(Guid value)
    {
        Value = value;
    }

    public static RoundId Create() => new(Guid.NewGuid());

    public static RoundId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RoundId cannot be empty.", nameof(value));

        return new RoundId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
