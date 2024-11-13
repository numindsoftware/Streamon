using System.Text.Json;

namespace Streamon.Serialization;

public class DefaultJsonSerializer(JsonSerializerOptions serializerOptions) : IJsonSerializer
{
    public static IJsonSerializer Instance { get; private set; } = new DefaultJsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    public static void SetDefaultSerializer(IJsonSerializer serializer) => Instance = serializer;

    public object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, serializerOptions) ?? throw new JsonException("Failed to deserialize, null results are not allowed");

    public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, serializerOptions) ?? throw new JsonException("Failed to deserialize, null results are not allowed");

    public string Serialize(object obj, bool indented = false) => JsonSerializer.Serialize(obj, serializerOptions);
}
