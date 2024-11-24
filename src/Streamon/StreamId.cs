namespace Streamon;

public readonly record struct StreamId(string Value) : IIdentity<StreamId, string>
{
    public static readonly StreamId Empty = new();
    public static StreamId From(string value) => new(value);
    public static StreamId For<T>() => new($"stream:{typeof(T).Name.ToLower()}:{Ulid.NewUlid()}");
    public override string ToString() => Value;
}
