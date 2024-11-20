using System.Text.Json;
using System.Text.Json.Serialization;

namespace Streamon;


public class IIdentityJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IIdentity<,>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[1];
        var converterType = typeof(IIdentityJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class IIdentityJsonConverter<T, V> : JsonConverter<T> where T : IIdentity<T, V>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueConverter = (JsonConverter<V>)options.GetConverter(typeof(V));
        return valueConverter == null
            ? throw new JsonException("Failed to get value converter")
            : T.From(valueConverter.Read(ref reader, typeof(V), options)!);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var valueConverter = (JsonConverter<V>)options.GetConverter(typeof(V));
        valueConverter.Write(writer, value.Value, options);
    }
}
