namespace Domain.BillingManagement.ValueObjects;

using Domain.Common;

public sealed class PlanAbonnementId : ValueObject
{
    public Guid Value { get; }

    private PlanAbonnementId(Guid value)
    {
        Value = value;
    }

    public static PlanAbonnementId Create() => new(Guid.NewGuid());

    public static PlanAbonnementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PlanAbonnementId cannot be empty.", nameof(value));

        return new PlanAbonnementId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
