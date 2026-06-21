using Azure;
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

/// <summary>
/// An <see cref="IEventInbox"/> backed by a single Azure Table. Each consumer (subscription +
/// handler) owns its own partition; rows are keyed by <see cref="EventId.Value"/> so the
/// existence check is a single point read.
/// </summary>
public class TableEventInboxStore(TableServiceClient tableServiceClient, string tableName) : IEventInbox
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient(tableName);

    /// <summary>Logical inbox scope incorporated into the partition key so multiple inboxes can share one table.</summary>
    /// <inheritdoc/>
    public async Task<bool> HasProcessedAsync(
        SubscriptionId subscriptionId,
        string consumerName,
        EventId eventId,
        CancellationToken cancellationToken = default)
    {
        if (!await tableServiceClient.CheckTableExistsAsync(tableName, cancellationToken).ConfigureAwait(false)) return false;

        var response = await _tableClient
            .GetEntityIfExistsAsync<InboxEntryEntity>(
                BuildPartitionKey(subscriptionId, consumerName),
                eventId.Value,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.HasValue;
    }

    /// <inheritdoc/>
    public async Task MarkProcessedAsync(
        SubscriptionId subscriptionId,
        string consumerName,
        Event @event,
        CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var entry = new InboxEntryEntity
        {
            PartitionKey = BuildPartitionKey(subscriptionId, consumerName),
            RowKey = @event.EventId.Value,
            GlobalPosition = @event.GlobalPosition.Value,
            StreamId = @event.StreamId.Value,
            ProcessedOn = DateTimeOffset.UtcNow,
        };

        try
        {
            // AddEntity surfaces duplicates explicitly — we swallow them so MarkProcessedAsync
            // remains idempotent per the IEventInbox contract.
            await _tableClient.AddEntityAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Already recorded by a concurrent delivery — nothing to do.
        }
    }

    private static string BuildPartitionKey(SubscriptionId subscriptionId, string consumerName) =>
        $"{subscriptionId.Value}|{consumerName}";
}