namespace Streamon.Subscription;

public interface IEventMiddleware
{
    Task InvokeAsync(Event @event, EventHandlerDelegate next, CancellationToken cancellationToken = default);
}