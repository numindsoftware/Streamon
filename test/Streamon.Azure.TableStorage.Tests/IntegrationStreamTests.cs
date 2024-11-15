using Streamon.Tests.Fixtures;
using System.Text.Json;

namespace Streamon.Azure.TableStorage.Tests;

public class IntegrationStreamTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    private readonly TableStreamStoreProvisioner _provisioner = new(containerFixture.TableServiceClient!, new StreamTypeProvider(new(JsonSerializerDefaults.Web)), new TableStreamStoreOptions() { TransactionBatchSize = 5 });

    [Fact]
    public async Task AppendsEventsToNewStream()
    {
        var store = await _provisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        IEnumerable<object> events = 
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped
        ];
        var stream = await store.AppendAsync(new StreamId("order-123"), StreamPosition.Start, events);
        Assert.NotEmpty(stream);
        Assert.Equal(events.Count(), stream.Count());
    }

    [Fact]
    public async Task AppendBatchedEventsToNewStream()
    {
        var store = await _provisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
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
        var stream = await store.AppendAsync(new StreamId("order-124"), StreamPosition.Start, events);
        Assert.NotEmpty(stream);
        Assert.Equal(events.Count(), stream.Count());
    }

    [Fact]
    public async Task FailsWhenAddingDuplicateEvents()
    {
        var store = await _provisioner.CreateStoreAsync(nameof(IntegrationStreamTests));
        IEnumerable<object> events =
        [
            new TestEvent1("1"),
            new TestEvent1("2"),
            new TestEvent2("3"),
            new TestEvent1("2") // this should fail
        ];
        var stream = await store.AppendAsync(new StreamId("order-125"), StreamPosition.Start, events);
        Assert.NotEmpty(stream);
        Assert.Equal(events.Count(), stream.Count());
    }

    public record TestEvent1([property: EventId] string Id);
    public record TestEvent2([property: EventId] string Id);
}
