namespace Domain.PaymentManagement.ValueObjects;

using Domain.Common;

/// <summary>
/// Value object representing a monetary amount in a specific currency.
/// Minimum amount is 100 FCFA. Only decimal type is used (never float/double).
/// </summary>
public sealed class Montant : ValueObject
{
    public const decimal MontantMinimum = 100m;

    public decimal Valeur { get; }
    public string Devise { get; }

    private Montant(decimal valeur, string devise)
    {
        Valeur = valeur;
        Devise = devise;
    }

    public static Montant Create(decimal valeur, string devise = "XOF")
    {
        if (valeur < MontantMinimum)
            throw new ArgumentException($"Le montant minimum est {MontantMinimum} {devise}.", nameof(valeur));

        if (string.IsNullOrWhiteSpace(devise))
            throw new ArgumentException("La devise ne peut pas être vide.", nameof(devise));

        return new Montant(valeur, devise);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valeur;
        yield return Devise;
    }

    public override string ToString() => $"{Valeur} {Devise}";
}
