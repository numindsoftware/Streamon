using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class ServiceCollectionSetupTests
{
    [Fact]
    public async Task CreateStoreThroughServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddStreamon().AddTableStorageEventStore("UseDevelopmentStorage=true");
        var provider = services.BuildServiceProvider();

        var provisioner = provider.GetRequiredService<IStreamStoreProvisioner>();
        var store = await provisioner.CreateStoreAsync();
        var stream = await store.AppendAsync(new StreamId("order-123"), StreamPosition.Start, [OrderEvents.OrderCaptured]);
        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }
}
