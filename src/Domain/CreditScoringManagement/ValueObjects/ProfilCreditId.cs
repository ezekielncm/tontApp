namespace Domain.CreditScoringManagement.ValueObjects;

using Domain.Common;

public sealed class ProfilCreditId : ValueObject
{
    public Guid Value { get; }

    private ProfilCreditId(Guid value)
    {
        Value = value;
    }

    public static ProfilCreditId Create() => new(Guid.NewGuid());

    public static ProfilCreditId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ProfilCreditId cannot be empty.", nameof(value));

        return new ProfilCreditId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
