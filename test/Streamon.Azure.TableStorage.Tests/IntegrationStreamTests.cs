using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;
using System.Text.Json;

namespace Streamon.Azure.TableStorage.Tests;

public class IntegrationStreamTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    private readonly TableStreamStoreProvisioner _provisioner = new(
        containerFixture.TableServiceClient!, 
        new (new StreamTypeProvider(new(JsonSerializerDefaults.Web))));

    [Fact]
    public async Task CreateStoreThroughServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddStreamon().AddTableStorageEventStore(containerFixture.TestContainer.GetConnectionString());
        var provider = services.BuildServiceProvider();

        var provisioner = provider.GetRequiredService<IStreamStoreProvisioner>();
        var store = await provisioner.CreateStoreAsync("TestCreated");
        var stream = await store.AppendAsync(new StreamId("order-123"), StreamPosition.Start, [OrderEvents.OrderCaptured]);
        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }

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
        await Assert.ThrowsAsync<DuplicateEventException>(() => store.AppendAsync(new StreamId("order-125"), StreamPosition.Start, events));
    }

    public record TestEvent1([property: EventId] string Id);
    public record TestEvent2([property: EventId] string Id);
}
