namespace Streamon.Subscription;

/// <summary>
/// Defines the primary contract for handling raw events delivered by a subscription.
/// </summary>
/// <remarks>Implementations receive the raw <see cref="Event"/> and are responsible for their own
/// dispatch logic. For strongly-typed dispatch, compose with <see cref="IEventHandler{TEvent}"/>
/// via <see cref="TypedEventHandler"/>.</remarks>
public interface IEventHandler
{
    Task HandleAsync(Event @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a contract for handling events of a specific type with a typed <see cref="EventHandlerContext{TEvent}"/>.
/// </summary>
/// <remarks>Register typed handlers via <see cref="StreamSubscriptionBuilder.AddTypedEventHandler{T}"/>
/// which composes them behind a <see cref="TypedEventHandler"/> and delivers a typed
/// <see cref="EventHandlerContext{TEvent}"/>.</remarks>
/// <typeparam name="TEvent">The type of event payload to handle.</typeparam>
public interface IEventHandler<TEvent>
{
    Task HandleAsync(EventHandlerContext<TEvent> context, CancellationToken cancellationToken = default);
}
