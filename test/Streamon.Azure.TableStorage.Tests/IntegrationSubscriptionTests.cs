using Azure.Data.Tables;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class IntegrationSubscriptionTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    private const string TableName = nameof(IntegrationSubscriptionTests);
    private static readonly SubscriptionId TestSubscriptionId = SubscriptionId.From("integration-sub");
    private static readonly StreamId TestStreamId = new("sub-order-1");

    [Fact, Priority(1)]
    public async Task SeedsEventsForSubscriptionTests()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(TableName, TestContext.Current.CancellationToken);
        IEnumerable<object> events =
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped
        ];
        var stream = await store.AppendEventsAsync(TestStreamId, StreamPosition.Start, events, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEmpty(stream);
        Assert.Equal(3, stream.Count());
    }

    [Fact, Priority(2)]
    public async Task CheckpointStoreReturnsEndWhenNoCheckpointExists()
    {
        var checkpointStore = CreateCheckpointStore();
        var checkpoint = await checkpointStore.GetCheckpointAsync(TestSubscriptionId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(StreamPosition.End, checkpoint);
    }

    [Fact, Priority(3)]
    public async Task CheckpointStorePersistsAndRetrievesPosition()
    {
        var checkpointStore = CreateCheckpointStore();
        var position = StreamPosition.From(42);

        await checkpointStore.SetCheckpointAsync(TestSubscriptionId, position, cancellationToken: TestContext.Current.CancellationToken);
        var retrieved = await checkpointStore.GetCheckpointAsync(TestSubscriptionId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(position, retrieved);
    }

    [Fact, Priority(4)]
    public async Task CheckpointStoreOverwritesExistingPosition()
    {
        var checkpointStore = CreateCheckpointStore();

        await checkpointStore.SetCheckpointAsync(TestSubscriptionId, StreamPosition.From(10), cancellationToken: TestContext.Current.CancellationToken);
        await checkpointStore.SetCheckpointAsync(TestSubscriptionId, StreamPosition.From(20), cancellationToken: TestContext.Current.CancellationToken);
        var retrieved = await checkpointStore.GetCheckpointAsync(TestSubscriptionId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(StreamPosition.From(20), retrieved);
    }

    [Fact, Priority(5)]
    public async Task SubscriptionStreamReaderFetchesEventsFromPosition()
    {
        var reader = CreateSubscriptionStreamReader();
        List<Event> events = [];

        await foreach (var @event in reader.FetchAsync(StreamPosition.Start, cancellationToken: TestContext.Current.CancellationToken))
        {
            events.Add(@event);
        }

        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.Equal(TestStreamId, e.StreamId));
    }

    [Fact, Priority(6)]
    public async Task SubscriptionStreamReaderReturnsLastGlobalPosition()
    {
        var reader = CreateSubscriptionStreamReader();
        var lastPosition = await reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(lastPosition.Value > 0);
    }

    [Fact, Priority(7)]
    public async Task SubscriptionStreamReaderSkipsEventsBeforePosition()
    {
        var reader = CreateSubscriptionStreamReader();
        List<Event> allEvents = [];
        await foreach (var @event in reader.FetchAsync(StreamPosition.Start, cancellationToken: TestContext.Current.CancellationToken))
        {
            allEvents.Add(@event);
        }

        Assert.True(allEvents.Count >= 2, "Need at least 2 events to test position filtering");

        var secondPosition = allEvents[1].GlobalPosition;
        List<Event> filteredEvents = [];
        await foreach (var @event in reader.FetchAsync(secondPosition, cancellationToken: TestContext.Current.CancellationToken))
        {
            filteredEvents.Add(@event);
        }

        Assert.True(filteredEvents.Count < allEvents.Count);
        Assert.All(filteredEvents, e => Assert.True(e.GlobalPosition.Value >= secondPosition.Value));
    }

    [Fact, Priority(8)]
    public async Task CatchUpSubscriptionInitializesCheckpointAtStart()
    {
        var subscriptionId = SubscriptionId.From("catchup-init-test");
        var checkpointStore = CreateCheckpointStore();
        var reader = CreateSubscriptionStreamReader();
        IEventHandler[] handlers = [new NoOpHandler()];

        StreamSubscription subscription = new(
            subscriptionId,
            StreamSubscriptionType.CatchUp,
            SubscriptionErrorHandling.Throw,
            checkpointStore,
            reader,
            (e, c) => Task.CompletedTask);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEqual(StreamPosition.End, checkpoint);
        Assert.True(checkpoint.Value >= 0);
    }

    [Fact, Priority(9)]
    public async Task LiveSubscriptionInitializesCheckpointAtEnd()
    {
        var subscriptionId = SubscriptionId.From("live-init-test");
        var checkpointStore = CreateCheckpointStore();
        var reader = CreateSubscriptionStreamReader();
        IEventHandler[] handlers = [];

        StreamSubscription subscription = new(
            subscriptionId,
            StreamSubscriptionType.Live,
            SubscriptionErrorHandling.Throw,
            checkpointStore,
            reader,
            (e, c) => Task.CompletedTask);

        var lastGlobalPosition = await reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEqual(StreamPosition.End, checkpoint);
        Assert.True(checkpoint.Value >= lastGlobalPosition.Value);
    }

    [Fact, Priority(10)]
    public async Task PollAdvancesCheckpointAfterProcessingEvents()
    {
        var subscriptionId = SubscriptionId.From("poll-advance-test");
        var checkpointStore = CreateCheckpointStore();
        var reader = CreateSubscriptionStreamReader();
        IEventHandler[] handlers = [];

        StreamSubscription subscription = new(
            subscriptionId,
            StreamSubscriptionType.CatchUp,
            SubscriptionErrorHandling.Throw,
            checkpointStore,
            reader,
            (e, c) => Task.CompletedTask);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);
        var checkpointAfterFirstPoll = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken: TestContext.Current.CancellationToken);

        // Append more events
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(TableName, cancellationToken: TestContext.Current.CancellationToken);
        await store.AppendEventsAsync(new StreamId("sub-order-2"), StreamPosition.Start, [OrderEvents.OrderCaptured], cancellationToken: TestContext.Current.CancellationToken);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);
        var checkpointAfterSecondPoll = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(checkpointAfterSecondPoll.Value >= checkpointAfterFirstPoll.Value);
    }

    [Fact, Priority(11)]
    public void SubscriptionManagerResolvesRegisteredSubscription()
    {
        var subscription = containerFixture.SubscriptionManager.Get(SubscriptionId.From("test-subscription"));
        Assert.NotNull(subscription);
    }

    [Fact, Priority(12)]
    public async Task SubscriptionStreamReaderSkipsDeletedStreams()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(TableName, cancellationToken: TestContext.Current.CancellationToken);
        var deletedStreamId = new StreamId("sub-deleted-stream");
        await store.AppendEventsAsync(deletedStreamId, StreamPosition.Start, [OrderEvents.OrderCaptured], cancellationToken: TestContext.Current.CancellationToken);
        await store.DeleteStreamAsync(deletedStreamId, StreamPosition.Any, cancellationToken: TestContext.Current.CancellationToken);

        var reader = CreateSubscriptionStreamReader();
        List<Event> events = [];
        await foreach (var @event in reader.FetchAsync(StreamPosition.Start, cancellationToken: TestContext.Current.CancellationToken))
        {
            events.Add(@event);
        }

        Assert.DoesNotContain(events, e => e.StreamId == deletedStreamId);
    }

    [Fact, Priority(13)]
    public async Task MultipleSubscriptionsTrackCheckpointsIndependently()
    {
        var subId1 = SubscriptionId.From("independent-sub-1");
        var subId2 = SubscriptionId.From("independent-sub-2");
        var checkpointStore = CreateCheckpointStore();

        await checkpointStore.SetCheckpointAsync(subId1, StreamPosition.From(5), cancellationToken: TestContext.Current.CancellationToken);
        await checkpointStore.SetCheckpointAsync(subId2, StreamPosition.From(15), cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint1 = await checkpointStore.GetCheckpointAsync(subId1, cancellationToken: TestContext.Current.CancellationToken);
        var checkpoint2 = await checkpointStore.GetCheckpointAsync(subId2, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(StreamPosition.From(5), checkpoint1);
        Assert.Equal(StreamPosition.From(15), checkpoint2);
    }

    private TableCheckpointStore CreateCheckpointStore() =>
        new(new TableClient(containerFixture.TestContainer.GetConnectionString(), TableCheckpointStore.DefaultCheckpointTableName), TableName);

    private TableSubscriptionStreamReader CreateSubscriptionStreamReader()
    {
        var options = new TableStreamStoreOptions
        {
            StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>()
        };
        return new(new TableClient(containerFixture.TestContainer.GetConnectionString(), TableName), options);
    }

    private class NoOpHandler : IEventHandler
    {
        public Task HandleAsync(Event @event, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
