namespace Domain.IdentityManagement.ValueObjects;

using Domain.Common;

public sealed class UtilisateurId : ValueObject
{
    public Guid Value { get; }

    private UtilisateurId(Guid value)
    {
        Value = value;
    }

    public static UtilisateurId Create() => new(Guid.NewGuid());

    public static UtilisateurId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UtilisateurId cannot be empty.", nameof(value));

        return new UtilisateurId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
