namespace Streamon.Subscription;

public interface IEventHandlerRegistry
{
    void RegisterHandlersFrom(Type handlersType);
    IReadOnlyCollection<EventHandlersEntry> GetHandlers(Type eventType);
}
