using System.Numerics;

namespace Streamon;

public readonly struct StreamPosition :
    IIdentity<StreamPosition, long>,
    IEquatable<StreamPosition>,
    IAdditionOperators<StreamPosition, StreamPosition, StreamPosition>,
    IIncrementOperators<StreamPosition>,
    IComparable<StreamPosition>,
    IEqualityOperators<StreamPosition, StreamPosition, bool>
{
    private StreamPosition(long value) => Value = value;

    public long Value { get; }

    public StreamPosition Next() => new(Value + 1);
    public static StreamPosition From(long value) => new(value >= -1 ? value : -1);

    public static readonly StreamPosition Any = new(-1);
    public static readonly StreamPosition Start = new(0);
    public static readonly StreamPosition End = new(long.MaxValue);

    public static StreamPosition operator +(StreamPosition left, StreamPosition right) =>
        left.Value == End.Value && right.Value == End.Value ? End : // avoids overflow, logically adding two ends is still end
        left.Value == Any.Value ? right : 
        right.Value == Any.Value ? left : 
        new(left.Value + right.Value);
    public static StreamPosition operator -(StreamPosition left, StreamPosition right) =>
        left.Value == Any.Value ? right :
        right.Value == Any.Value ? left :
        new(left.Value + right.Value);

    public static bool operator >(StreamPosition left, StreamPosition right) => !(left.Value == Any.Value || right.Value == Any.Value) && left.Value > right.Value;
    public static bool operator <(StreamPosition left, StreamPosition right) => !(left.Value == Any.Value || right.Value == Any.Value) && left.Value < right.Value;
    public static bool operator >=(StreamPosition left, StreamPosition right) => left.Value == Any.Value || right.Value == Any.Value || left.Value >= right.Value;
    public static bool operator <=(StreamPosition left, StreamPosition right) => left.Value == Any.Value || right.Value == Any.Value || left.Value <= right.Value;
    public static StreamPosition operator ++(StreamPosition value) => value.Next();

    public static bool operator ==(StreamPosition left, StreamPosition right) => 
        left.Value == Any.Value || right.Value == Any.Value || left.Value == right.Value;
    public static bool operator !=(StreamPosition left, StreamPosition right) => 
        left.Value != Any.Value && right.Value != Any.Value && left.Value != right.Value;

    public int CompareTo(StreamPosition other) => (Value == Any.Value || other.Value == Any.Value) ? 0 : Value.CompareTo(other.Value);
    public bool Equals(StreamPosition other) => Value == Any.Value || other.Value == Any.Value || Value == other.Value;
    public override bool Equals(object? obj) => obj is StreamPosition other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
