namespace Streamon.Subscription;

public record EventHandlersEntry(Type HandlerType, Func<object, object, CancellationToken, Task> Handler);