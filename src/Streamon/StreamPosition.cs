namespace Streamon;

public readonly record struct StreamPosition(long Value = default)
{
    public static readonly StreamPosition Any = new(-1);
    public static readonly StreamPosition Start = new(0);
    public static readonly StreamPosition End = new(long.MaxValue);
    public StreamPosition Next() => new(Value + 1);
    public static explicit operator long(StreamPosition id) => id.Value;
    public static explicit operator StreamPosition(long value) => new(value);
    public static StreamPosition operator +(StreamPosition sequence, long value) => new(sequence.Value + value);
    public static StreamPosition operator -(StreamPosition position, long value) => new(position.Value - value);
    public static StreamPosition operator +(StreamPosition left, StreamPosition right) => new(left.Value + right.Value);
    public static StreamPosition operator -(StreamPosition left, StreamPosition right) => new(left.Value - right.Value);
    public static bool operator >(StreamPosition left, StreamPosition right) => left.Value > right.Value;
    public static bool operator <(StreamPosition left, StreamPosition right) => left.Value < right.Value;
    public static bool operator >=(StreamPosition left, StreamPosition right) => left.Value >= right.Value;
    public static bool operator <=(StreamPosition left, StreamPosition right) => left.Value <= right.Value;
    public override string ToString() => Value.ToString();
}
