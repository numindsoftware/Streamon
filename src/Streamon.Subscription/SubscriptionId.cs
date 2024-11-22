namespace Streamon.Subscription;

public record SubscriptionId(string Value) : IIdentity<SubscriptionId, string>
{
    public static SubscriptionId From(string value) => new(value);

    public static SubscriptionId New() => new(Ulid.NewUlid().ToString());

    override public string ToString() => Value;
}
