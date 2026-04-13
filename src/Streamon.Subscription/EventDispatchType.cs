namespace Streamon.Subscription;

public enum EventDispatchType
{
       /// <summary>
    /// Dispatches events to handlers in the order they were registered, awaiting each handler before invoking the next.
    /// </summary>
    Sequential,
    /// <summary>
    /// Dispatches events to all handlers concurrently and awaits their completion before proceeding.
    /// </summary>
    Concurrent
}