namespace Streamon;

public interface IStreamTypeProvider
{
    object ResolveEvent(string name, string data);
    EventTypeInfo SerializeEvent(object @event);

    EventMetadata? ResolveMetadata(string data);
    string SerializeMetadata(EventMetadata? metadata);
}
