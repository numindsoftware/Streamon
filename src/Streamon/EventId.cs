using System.Text.Json.Serialization;

namespace Streamon;

[JsonConverter(typeof(EventIdJsonConverter))]
public readonly record struct EventId(string Value)
{
    public static EventId New() => new(Ulid.NewUlid().ToString());
    public static EventId From(string value) => new(value);
    public static explicit operator EventId(string value) => new(value);
    public static explicit operator string(EventId id) => id.Value;
    public override string ToString() => Value;
}
