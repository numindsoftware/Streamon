
namespace Streamon;

[Serializable]
public class EventTypeNotFoundException(string typeName, string? message = default) : Exception(message)
{
    public string TypeName { get; } = typeName;
}