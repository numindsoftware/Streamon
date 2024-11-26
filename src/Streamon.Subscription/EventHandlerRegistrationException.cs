namespace Streamon.Subscription;

public class EventHandlerRegistrationException(Type type, string? message = default, Exception? innerException = default) : Exception(message, innerException)
{
    public Type Type { get; } = type;
}
