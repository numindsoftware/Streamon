namespace Streamon;

[Serializable]
public class StreamTypeProviderException(string typeName, Type type, string? message = default, Exception? innerException = default) : Exception(message, innerException)
{
    public string EntityTypeName { get; } = typeName;
    public Type EntityType { get; } = type;
}
