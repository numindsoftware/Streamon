using Microsoft.Extensions.DependencyInjection;
using Streamon.Memory;
using Streamon.Tests.Fixtures;

namespace Streamon.Tests;

public class InMemoryStreamStoreTests
{
    [Fact]
    public async Task CorrectlyAppendsNewEvents()
    {
        MemoryStreamStore eventStore = new();

        StreamId streamId = new("order-123");
        IEnumerable<object> events = [OrderEvents.OrderCaptured, OrderEvents.OrderConfirmed];

        var stream = await eventStore.AppendAsync(streamId, StreamPosition.Start, events);

        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
        Assert.Equal(stream.GlobalPosition, stream.CurrentPosition);
    }

    [Fact]
    public async Task CreateStoreThroughServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddStreamon().AddMemoryEventStore();
        var provider = services.BuildServiceProvider();

        var provisioner = provider.GetRequiredService<IStreamStoreProvisioner>();
        var store = await provisioner.CreateStoreAsync();
        var stream = await store.AppendAsync(new StreamId("order-123"), StreamPosition.Start, [OrderEvents.OrderCaptured]);
        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }
}

