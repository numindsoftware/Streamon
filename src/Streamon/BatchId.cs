namespace Streamon;

public readonly record struct BatchId(string Value) : IIdentity<BatchId, string>
{
    public static BatchId New() => new(Ulid.NewUlid().ToString());
    public static BatchId From(string value) => new(value);
    public override string ToString() => Value;
}
