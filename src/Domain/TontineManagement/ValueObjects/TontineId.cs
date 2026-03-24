namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class TontineId : ValueObject
{
    public Guid Value { get; }

    private TontineId(Guid value)
    {
        Value = value;
    }

    public static TontineId Create() => new(Guid.NewGuid());

    public static TontineId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TontineId cannot be empty.", nameof(value));

        return new TontineId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
