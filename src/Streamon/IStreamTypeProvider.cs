using System.Reflection;

namespace Streamon;

public interface IStreamTypeProvider
{
    IStreamTypeProvider RegisterTypes(Assembly assembly);

    object ResolveEvent(string name, string data);
    EventTypeInfo SerializeEvent(object @event);

    EventMetadata? ResolveMetadata(string? data);
    string? SerializeMetadata(EventMetadata? metadata);
}
