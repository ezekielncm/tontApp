namespace Domain.NotificationManagement.ValueObjects;

using Domain.Common;

public sealed class NotificationId : ValueObject
{
    public Guid Value { get; }

    private NotificationId(Guid value)
    {
        Value = value;
    }

    public static NotificationId Create() => new(Guid.NewGuid());

    public static NotificationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("NotificationId cannot be empty.", nameof(value));

        return new NotificationId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
