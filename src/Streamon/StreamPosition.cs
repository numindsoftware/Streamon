using System.Numerics;

namespace Streamon;

public readonly record struct StreamPosition(long Value = default) :
    IAdditionOperators<StreamPosition, StreamPosition, StreamPosition>,
    //IAdditionOperators<StreamPosition, long, StreamPosition>,
    //IAdditionOperators<long, StreamPosition, StreamPosition>,
    //IAdditionOperators<int, StreamPosition, int>,
    //IAdditionOperators<StreamPosition, int, StreamPosition>,
    //IAdditionOperators<int, StreamPosition, StreamPosition>,
    //ISubtractionOperators<StreamPosition, StreamPosition, StreamPosition>,
    //ISubtractionOperators<StreamPosition, long, StreamPosition>,
    //ISubtractionOperators<long, StreamPosition, StreamPosition>,
    //ISubtractionOperators<StreamPosition, int, StreamPosition>,
    //ISubtractionOperators<int, StreamPosition StreamPosition>,
    IIncrementOperators<StreamPosition>,
    IEqualityOperators<StreamPosition, StreamPosition, bool>
{
    public static readonly StreamPosition Any = new(-1);
    public static readonly StreamPosition Start = new(0);
    public static readonly StreamPosition End = new(long.MaxValue);
    public StreamPosition Next() => new(Value + 1);

    public static explicit operator long(StreamPosition id) => id.Value;
    public static explicit operator StreamPosition(long value) => new(value);

    public static StreamPosition operator +(StreamPosition left, StreamPosition right) => new(left.Value + right.Value);
    public static StreamPosition operator -(StreamPosition left, StreamPosition right) => new(left.Value - right.Value);

    //public static StreamPosition operator +(StreamPosition position, long value) => new(position.Value + value);
    //public static StreamPosition operator -(StreamPosition position, long value) => new(position.Value - value);
    //public static StreamPosition operator +(StreamPosition position, int value) => new(position.Value + value);
    //public static StreamPosition operator -(StreamPosition position, int value) => new(position.Value - value);
    //public static StreamPosition operator +(StreamPosition left, StreamPosition right) => new(left.Value + right.Value);
    //public static StreamPosition operator -(StreamPosition left, StreamPosition right) => new(left.Value - right.Value);

    //public static StreamPosition operator +(long value, StreamPosition position) => new(value + position.Value);
    //public static StreamPosition operator -(long value, StreamPosition position) => new(value - position.Value);
    //public static StreamPosition operator +(int value, StreamPosition position) => new(value + position.Value);
    //public static StreamPosition operator -(int value, StreamPosition position) => new(value - position.Value);

    public static bool operator >(StreamPosition left, StreamPosition right) => left.Value > right.Value;
    public static bool operator <(StreamPosition left, StreamPosition right) => left.Value < right.Value;
    public static bool operator >=(StreamPosition left, StreamPosition right) => left.Value >= right.Value;
    public static bool operator <=(StreamPosition left, StreamPosition right) => left.Value <= right.Value;
    public static StreamPosition operator ++(StreamPosition value) => value.Next();

    public override string ToString() => Value.ToString();
}
