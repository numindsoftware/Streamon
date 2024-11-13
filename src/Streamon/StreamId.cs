namespace Streamon;

public readonly record struct StreamId(string Value)
{
    public static readonly StreamId Empty = new();
    public static StreamId For<T>() => new($"stream:{typeof(T).Name.ToLower()}:{Ulid.NewUlid()}");
    public static explicit operator StreamId(string value) => new(value);
    public static explicit operator string(StreamId id) => id.Value;
    public override string ToString() => Value;
}
