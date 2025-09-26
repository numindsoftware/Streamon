namespace Streamon.Subscription;

public interface IEventHandlerRegistry
{
    void RegisterHandlersFrom(Type handlersType);
    IReadOnlyCollection<EventHandlersEntry> GetHandlers(Type eventType);
}

public record EventHandlersEntry(Type HandlerType, Func<object, object, CancellationToken, Task> Handler);