using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

public class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => 
        TestContainer = new AzuriteBuilder()
        .WithName("streamon-azurite")
        .WithPortBinding(10002, true)
        .Build();

    public async Task DisposeAsync() => 
        await TestContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        ServiceCollection services = new();

        await TestContainer.StartAsync();


        services.AddStreamon().AddTableStorageStreamStore(TestContainer.GetConnectionString(), options =>
        {
            options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
        });

        ServiceProvider = services.BuildServiceProvider();

        TableStreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public AzuriteContainer TestContainer { get; private set; }

    public IStreamStoreProvisioner TableStreamStoreProvisioner { get; private set; } = null!;
}
