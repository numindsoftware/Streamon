namespace Streamon.Subscription;

public interface IEventHandler
{
    public ValueTask HandleEventAsync(IEventConsumeContext context, CancellationToken cancellationToken = default);
}
