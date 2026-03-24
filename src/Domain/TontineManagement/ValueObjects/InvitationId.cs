namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

public sealed class InvitationId : ValueObject
{
    public Guid Value { get; }

    private InvitationId(Guid value)
    {
        Value = value;
    }

    public static InvitationId Create() => new(Guid.NewGuid());

    public static InvitationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("InvitationId cannot be empty.", nameof(value));

        return new InvitationId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
