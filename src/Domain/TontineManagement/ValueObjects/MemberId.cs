namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class MemberId : ValueObject
{
    public Guid Value { get; }

    private MemberId(Guid value)
    {
        Value = value;
    }

    public static MemberId Create() => new(Guid.NewGuid());

    public static MemberId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MemberId cannot be empty.", nameof(value));

        return new MemberId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
