using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

public class ProjectionFixture : IAsyncLifetime
{
    public const string StreamTableName = nameof(ProjectionTests);
    public const string ProjectionTableName = "OrderProjections";

    public ProjectionFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-projections")
            .WithPortBinding(10002, true)
            .Build();

    public async ValueTask DisposeAsync() =>
        await TestContainer.DisposeAsync();

    public async ValueTask InitializeAsync()
    {
        ServiceCollection services = new();

        await TestContainer.StartAsync();

        var connectionString = TestContainer.GetConnectionString();

        var typeProvider = new StreamTypeProvider()
            .RegisterTypes<OrderCaptured>()
            .RegisterTypes<OrderDetailsUpdated>();

        services.AddStreamon()
            .AddStreamTypeProvider([typeof(ProjectionFixture).Assembly])
            .AddTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTypeProvider = typeProvider;
            });

        services.AddStreamSubscription(SubscriptionId.From("projection-sub"), StreamSubscriptionType.CatchUp)
            .UseTableStorageCheckpointStore(connectionString, StreamTableName)
            .UseTableStorageSubscriptionStreamReader(connectionString, StreamTableName, options =>
            {
                options.StreamTypeProvider = typeProvider;
            })
            .AddTableStorageProjection<OrderTableProjector, OrderProjectionEntity>(
                connectionString,
                ProjectionTableName,
                partitionKeySelector: s => s.OrderId,
                rowKeySelector: s => s.OrderId);

        ServiceProvider = services.BuildServiceProvider();

        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionManager = ServiceProvider.GetRequiredService<SubscriptionManager>();

        // Expose the same projection store used by the subscription pipeline so tests
        // read projections through the same serialization/deserialization path as clients.
        ProjectionStore = new TableStorageProjectionStore<OrderProjectionEntity>(
            new TableClient(connectionString, ProjectionTableName),
            partitionKeySelector: s => s.OrderId,
            rowKeySelector: s => s.OrderId);
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public AzuriteContainer TestContainer { get; private set; }
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public SubscriptionManager SubscriptionManager { get; private set; } = null!;
    public IProjectionStore<OrderProjectionEntity> ProjectionStore { get; private set; } = null!;
}