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
        TestContainer = new AzuriteBuilder()
            .WithName("streamon-azurite-projections")
            .WithPortBinding(10002, true)
            .Build();

    public async Task DisposeAsync() =>
        await TestContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        ServiceCollection services = new();

        await TestContainer.StartAsync();

        var connectionString = TestContainer.GetConnectionString();

        services.AddStreamon()
            .AddStreamTypeProvider([typeof(ProjectionFixture).Assembly])
            .AddTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
            });

        services.AddStreamSubscription(SubscriptionId.From("projection-sub"), StreamSubscriptionType.CatchUp)
            .UseTableStorageCheckpointStore(connectionString, StreamTableName)
            .UseTableStorageSubscriptionStreamReader(connectionString, StreamTableName, options =>
            {
                options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
            })
            .AddTableStorageProjection<OrderTableProjector, OrderProjectionEntity>(
                connectionString,
                ProjectionTableName,
                partitionKeySelector: s => s.OrderId,
                rowKeySelector: s => s.OrderId);

        ServiceProvider = services.BuildServiceProvider();

        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionManager = ServiceProvider.GetRequiredService<SubscriptionManager>();
        ProjectionTableClient = new TableClient(connectionString, ProjectionTableName);
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public AzuriteContainer TestContainer { get; private set; }
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public SubscriptionManager SubscriptionManager { get; private set; } = null!;
    public TableClient ProjectionTableClient { get; private set; } = null!;
}