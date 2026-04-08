namespace Domain.CreditScoringManagement.ValueObjects;

using Domain.Common;

public sealed class HistoriqueComportementId : ValueObject
{
    public Guid Value { get; }

    private HistoriqueComportementId(Guid value)
    {
        Value = value;
    }

    public static HistoriqueComportementId Create() => new(Guid.NewGuid());

    public static HistoriqueComportementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("HistoriqueComportementId cannot be empty.", nameof(value));

        return new HistoriqueComportementId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
