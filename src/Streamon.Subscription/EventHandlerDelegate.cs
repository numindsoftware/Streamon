namespace Streamon.Subscription;

public delegate Task EventHandlerDelegate(Event @event, CancellationToken cancellationToken);
