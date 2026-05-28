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
    public const string StreamCheckpointTableName = StreamTableName + "Checkpoints";

    public ProjectionFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-projections")
            .WithPortBinding(10002, true)
            .Build();

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync() =>
        await TestContainer.DisposeAsync();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    public async ValueTask InitializeAsync()
    {
        ServiceCollection services = new();

        await TestContainer.StartAsync();

        var connectionString = TestContainer.GetConnectionString();

        var typeProvider = new StreamTypeProvider()
            .RegisterTypes<OrderCaptured>()
            .RegisterTypes<OrderDetailsUpdated>();

        services.AddStreamon()
            .UseTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTableName = StreamTableName;
                options.StreamTypeProvider = typeProvider;
            });

        services.AddStreamonSubscription(SubscriptionId.From("projection-sub"), o => o.StreamSubscriptionType = StreamSubscriptionType.CatchUp)
            .UseTableStorageCheckpointStore(connectionString, options =>
            {
                options.CheckpointTableName = StreamCheckpointTableName;
            })
            .UseTableStorageSubscriptionStreamReader(connectionString, options =>
            {
                options.StreamTableName = StreamTableName;
                options.StreamTypeProvider = typeProvider;
            })
            .AddTableStorageProjection<OrderTableProjector, OrderProjectionEntity>(connectionString);

        ServiceProvider = services.BuildServiceProvider();

        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionProvisioner = ServiceProvider.GetRequiredService<IStreamSubscriptionProvisioner>();
    }

    public IProjectionStore<TState> CreateProjectionStore<TState>(string name)
        where TState : class, ITableEntity, new()
    {
        TableProjectionStoreOptions options = new();
        var connectionString = TestContainer.GetConnectionString();
        return new TableProjectionStore<TState>(
            new TableClient(connectionString, options.ComposeProjectionTableName<TState>(name)), 
            options.JsonSerializerOptions);
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public AzuriteContainer TestContainer { get; private set; }
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public IStreamSubscriptionProvisioner SubscriptionProvisioner { get; private set; } = null!;
}