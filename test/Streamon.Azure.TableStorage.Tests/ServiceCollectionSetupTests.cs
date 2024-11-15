using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        var stream = await store.AppendAsync(new StreamId("order-123"), StreamPosition.Start, [new OrderCaptured("1")]);
        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }

}
