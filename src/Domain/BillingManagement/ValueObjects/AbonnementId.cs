namespace Domain.BillingManagement.ValueObjects;

using Domain.Common;

public sealed class AbonnementId : ValueObject
{
    public Guid Value { get; }

    private AbonnementId(Guid value)
    {
        Value = value;
    }

    public static AbonnementId Create() => new(Guid.NewGuid());

    public static AbonnementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AbonnementId cannot be empty.", nameof(value));

        return new AbonnementId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
