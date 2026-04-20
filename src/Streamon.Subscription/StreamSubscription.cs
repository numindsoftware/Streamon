namespace Streamon.Subscription;

public class StreamSubscription(
    SubscriptionId subscriptionId,
    StreamSubscriptionType streamSubscriptionType,
    SubscriptionErrorHandling errorHandling,
    ICheckpointStore checkpointStore,
    ISubscriptionStreamReader subscriptionStreamReader,
    EventHandlerDelegate pipeline)
{
    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        var lastCheckpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (lastCheckpoint == StreamPosition.End)
        {
            lastCheckpoint = streamSubscriptionType == StreamSubscriptionType.CatchUp
                ? StreamPosition.Start
                : await subscriptionStreamReader.GetLastGlobalPositionAsync(cancellationToken).ConfigureAwait(false);
            await checkpointStore.SetCheckpointAsync(subscriptionId, lastCheckpoint, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var @event in subscriptionStreamReader.FetchAsync(lastCheckpoint.Next(), cancellationToken).ConfigureAwait(false))
        {
            var globalPosition = @event.GlobalPosition;
            try
            {
                await pipeline(@event, cancellationToken).ConfigureAwait(false);
                globalPosition = await subscriptionStreamReader.GetLastGlobalPositionAsync(cancellationToken).ConfigureAwait(false);
                await checkpointStore.SetCheckpointAsync(subscriptionId, globalPosition, cancellationToken).ConfigureAwait(false);
            }
            catch when (errorHandling == SubscriptionErrorHandling.Ignore)
            {
                await checkpointStore.SetCheckpointAsync(subscriptionId, globalPosition, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
