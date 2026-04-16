namespace Streamon.Subscription;

public record StreamSubscriptionName(string Value) : IIdentity<SubscriptionId, string>
{
    public static SubscriptionId From(string value) => new(value);
    override public string ToString() => Value;
}