namespace Streamon.Subscription;

public interface IEventHandler
{
    ValueTask HandleEventAsync(EventConsumeContext<object> context, CancellationToken cancellationToken = default);
}

public interface IEventHandler<T>
{
    ValueTask HandleEventAsync(EventConsumeContext<T> context, CancellationToken cancellationToken = default);
}