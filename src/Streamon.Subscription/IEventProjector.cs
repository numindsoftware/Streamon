namespace Streamon.Subscription;

public interface IEventProjector
{
    Task ProjectAsync(Event @event, CancellationToken cancellationToken = default);
}
