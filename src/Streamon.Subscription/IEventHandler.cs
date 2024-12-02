namespace Streamon.Subscription;

public interface IEventHandler
{
    ValueTask HandleAsync(EventConsumeContext<object> context, CancellationToken cancellationToken = default);
}

public interface IStreamEventHandler<T>
{
    ValueTask HandleAsync(EventConsumeContext<T> context, CancellationToken cancellationToken = default);
}