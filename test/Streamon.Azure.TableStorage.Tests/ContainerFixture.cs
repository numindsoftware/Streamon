using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

public class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => 
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithName("streamon-azurite")
        .Build();

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync() => 
        await TestContainer.DisposeAsync();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    public async ValueTask InitializeAsync()
    {
        ServiceCollection services = new();

        await TestContainer.StartAsync();

        services.AddStreamon()
            .UseTableStorageStreamStore(TestContainer.GetConnectionString(), options =>
            {
                options.StreamTableName = string.Empty;
                options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
            });

        services.AddStreamonSubscription(SubscriptionId.From("test-subscription"))
            .UseTableStorageCheckpointStore(TestContainer.GetConnectionString(), o => o.CheckpointTableName = nameof(IntegrationStreamTests))
            .UseTableStorageSubscriptionStreamReader(TestContainer.GetConnectionString(), o => o.StreamTableName = nameof(IntegrationStreamTests));

        ServiceProvider = services.BuildServiceProvider();

        TableStreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionProvisioner = ServiceProvider.GetRequiredService<IStreamSubscriptionProvisioner>();
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public AzuriteContainer TestContainer { get; private set; }

    public IStreamStoreProvisioner TableStreamStoreProvisioner { get; private set; } = null!;

    public IStreamSubscriptionProvisioner SubscriptionProvisioner { get; private set; } = null!;
}

public record OrderProjection(string Id, OrderProduct OrderProduct, DateTimeOffset CreatedOn, DateTimeOffset? CancelledOn = default, string? CancelledBy = default, string? CancellationReason = default);
public record OrderProduct(string Name, decimal Price);