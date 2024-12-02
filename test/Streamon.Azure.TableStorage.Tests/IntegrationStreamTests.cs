using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

[TestCaseOrderer("Streamon.Tests.Fixtures.PriorityTestCollectionOrderer", "Streamon.Tests.Fixtures")]
public class IntegrationStreamTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    [Fact, Priority(1)]
    public async Task CreateStoreThroughServiceCollection()
    {

        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync("TestCreated");
        var stream = await store.AppendEventsAsync(new StreamId("order-123"), StreamPosition.Start, [OrderEvents.OrderCaptured]);
        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }

    [Fact, Priority(2)]
    public async Task AppendsEventsToNewStream()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        IEnumerable<object> events = 
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped
        ];
        var stream = await store.AppendEventsAsync(new StreamId("order-123"), StreamPosition.Start, events);
        Assert.NotEmpty(stream);
        Assert.Equal(events.Count(), stream.Count());
    }

    [Fact, Priority(3)]
    public async Task AppendBatchedEventsToNewStream()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        IEnumerable<object> events =
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped,
            OrderEvents.OrderShipped,
            OrderEvents.OrderFulfilled,
            OrderEvents.OrderFulfilled,
            OrderEvents.OrderCancelled,
            OrderEvents.OrderCancelled
        ];
        var stream = await store.AppendEventsAsync(new StreamId("order-124"), StreamPosition.Start, events);
        Assert.NotEmpty(stream);
        Assert.Equal(events.Count(), stream.Count());
    }

    [Fact, Priority(5)]
    public async Task FailsWhenAddingDuplicateEvents()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        IEnumerable<object> events =
        [
            new TestEvent1("1"),
            new TestEvent1("2"),
            new TestEvent2("3"),
            new TestEvent1("2") // this should fail
        ];
        await Assert.ThrowsAsync<DuplicateEventException>(() => store.AppendEventsAsync(new StreamId("order-125"), StreamPosition.Start, events));
    }

    [Fact, Priority(6)]
    public async Task ReadsFullStreamFromStoreEvents()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        var readStream = await store.FetchEventsAsync(new StreamId("order-124"));
        Assert.NotEmpty(readStream);
        Assert.Equal(9, readStream.Count());
    }

    //[Fact, Priority(7)]
    //public async Task ProjectEventsForStream(StreamId streamId, IEnumerable<object> events)
    //{
    //    var subscriptionId = SubscriptionId.New();

    //    ServiceCollection services = new();
    //    services.AddStreamSubscription(subscriptionId, StreamSubscriptionType.CatchUp)
    //        .AddTableStorageCheckpointStore(containerFixture.TestContainer.GetConnectionString())
    //        .AddTableStorageSubscriptionStreamReader("")
    //        .AddEventHandler<OrderInMemoryProjector>();

    //    var provider = services.BuildServiceProvider();
        
    //    await store.AppendEventsAsync(streamId, StreamPosition.Start, events);
    //}

    [Fact, Priority(8)]
    public async Task SoftDeletesExistingStream()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        var totalDeleted = await store.DeleteStreamAsync(new StreamId("order-124"), StreamPosition.Any);
        Assert.Equal(9, totalDeleted);
    }

    [Fact, Priority(9)]
    public async Task HardDeletesExistingStream()
    {
        var store = await containerFixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        var totalDeleted = await store.DeleteStreamAsync(new StreamId("order-123"), StreamPosition.Any);
        Assert.Equal(3, totalDeleted);
    }

    public record TestEvent1([property: EventId] string Id);
    public record TestEvent2([property: EventId] string Id);
}

public class OrderInMemoryProjector : IEventHandler
{
    public ValueTask HandleEventAsync(EventConsumeContext<object> context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}