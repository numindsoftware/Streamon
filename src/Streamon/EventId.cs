using System.Text.Json.Serialization;

namespace Streamon;

[JsonConverter(typeof(EventIdJsonConverter))]
public readonly record struct EventId(string Value) : IIdentity<EventId, string>
{
    public static EventId New() => new(Ulid.NewUlid().ToString());
    public static EventId From(string value) => new(value);
    public override string ToString() => Value;
}
