namespace Streamon.Subscription;

public class EventPipelineBuilder
{
    private readonly IList<EventPipelineDelegate> _components = [];

    /// <summary>
    /// Adds an inline middleware function to the pipeline.
    /// </summary>
    public EventPipelineBuilder Use(Func<Event, EventHandlerDelegate, CancellationToken, Task> middleware)
    {
        _components.Add(next => (@event, cancellationToken) => middleware(@event, next, cancellationToken));
        return this;
    }

    /// <summary>
    /// Adds an <see cref="IEventMiddleware"/> to the pipeline, resolved on each invocation via the
    /// supplied <paramref name="factory"/>. The factory is captured at registration time, so the
    /// caller controls how the instance is obtained (e.g. from DI, manual construction, etc.).
    /// </summary>
    public EventPipelineBuilder UseMiddleware<TMiddleware>(Func<TMiddleware> factory)
        where TMiddleware : IEventMiddleware
    {
        _components.Add(next => async (@event, cancellationToken) =>
        {
            var middleware = factory();
            await middleware.InvokeAsync(@event, next, cancellationToken);
        });
        return this;
    }

    /// <summary>
    /// Builds the pipeline by folding all registered components over the <paramref name="termination"/> delegate.
    /// Components are folded in reverse so that the first registered middleware is the outermost wrapper,
    /// matching ASP.NET Core's middleware execution convention.
    /// </summary>
    public EventHandlerDelegate Build(EventHandlerDelegate termination) =>
        _components.Reverse().Aggregate(termination, (next, component) => component(next));
}
