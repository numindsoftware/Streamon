namespace Streamon.Subscription;

public interface IEventAsyncHandler<TEvent>
{
    ValueTask HandleAsync(EventConsumeContext<TEvent> context, CancellationToken cancellationToken = default);
}
