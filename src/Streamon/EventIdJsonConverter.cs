using System.Text.Json;
using System.Text.Json.Serialization;

namespace Streamon;

internal class EventIdJsonConverter : JsonConverter<EventId>
{
    public override EventId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new (reader.GetString() ?? throw new JsonException("Failed to read EventId"));
    
    public override void Write(Utf8JsonWriter writer, EventId value, JsonSerializerOptions options) => 
        writer.WriteStringValue(value.Value);
}