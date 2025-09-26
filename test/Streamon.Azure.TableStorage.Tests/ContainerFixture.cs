using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
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

        services.AddStreamon()
            .AddTableStorageStreamStore(TestContainer.GetConnectionString(), options =>
            {
                options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
            });

        services.AddStreamSubscription(SubscriptionId.From("test-subscription"), StreamSubscriptionType.CatchUp)
            .UseTableStorageCheckpointStore(TestContainer.GetConnectionString(), nameof(IntegrationStreamTests))
            .UseTableStorageSubscriptionStreamReader(TestContainer.GetConnectionString(), nameof(IntegrationStreamTests))
            .AddEventHandler<OrderInMemoryProjector>();

        ServiceProvider = services.BuildServiceProvider();

        TableStreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionManager = ServiceProvider.GetRequiredService<SubscriptionManager>();
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public AzuriteContainer TestContainer { get; private set; }

    public IStreamStoreProvisioner TableStreamStoreProvisioner { get; private set; } = null!;

    public SubscriptionManager SubscriptionManager { get; private set; } = null!;
}

public record OrderProjection(string Id, OrderProduct OrderProduct, DateTimeOffset CreatedOn, DateTimeOffset? CancelledOn = default, string? CancelledBy = default, string? CancellationReason = default);
public record OrderProduct(string Name, decimal Price);