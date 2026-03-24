namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class ContributionAmount : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private ContributionAmount(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static ContributionAmount Create(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new ArgumentException("Contribution amount must be greater than zero.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must not be empty.", nameof(currency));

        return new ContributionAmount(amount, currency.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
