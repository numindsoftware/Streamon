using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

/// <summary>
/// Functional tests proving that projections implementing <see cref="IProjectionTrackable"/>
/// are stamped with the last applied <c>GlobalPosition</c> and short-circuit duplicate
/// deliveries — the per-projection idempotency story under at-least-once semantics.
/// </summary>
public class ProjectionTrackingFixture : IAsyncLifetime
{
    public const string StreamTableName = nameof(ProjectionTrackingTests);
    public const string CheckpointTableName = nameof(ProjectionTrackingTests) + "Checkpoints";
    public static readonly SubscriptionId SubscriptionId = SubscriptionId.From("tracked-projection-sub");

    public ProjectionTrackingFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-tracked-projections")
            .Build();

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync() => await TestContainer.DisposeAsync();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    public async ValueTask InitializeAsync()
    {
        await TestContainer.StartAsync();
        var connectionString = TestContainer.GetConnectionString();

        var typeProvider = new StreamTypeProvider()
            .RegisterTypes<OrderCaptured>();

        var services = new ServiceCollection();

        services.AddStreamon()
            .UseTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTableName = StreamTableName;
                options.StreamTypeProvider = typeProvider;
            });

        services.AddStreamonSubscription(SubscriptionId, o => o.StreamSubscriptionType = StreamSubscriptionType.CatchUp)
            .UseTableStorageCheckpointStore(connectionString, o =>
            {
                o.CheckpointTableName = CheckpointTableName;
            })
            .UseTableStorageSubscriptionStreamReader(connectionString, options =>
            {
                options.StreamTableName = StreamTableName;
                options.StreamTypeProvider = typeProvider;
            })
            .AddTableStorageProjection<TrackedOrderProjector, TrackedOrderProjection>(connectionString);

        ServiceProvider = services.BuildServiceProvider();
        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionProvisioner = ServiceProvider.GetRequiredService<IStreamSubscriptionProvisioner>();
    }

    public AzuriteContainer TestContainer { get; }
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public IStreamSubscriptionProvisioner SubscriptionProvisioner { get; private set; } = null!;
    //public IProjectionStore<TrackedOrderProjection> ProjectionStore { get; private set; } = null!;

    public IProjectionStore<TState> CreateProjectionStore<TState>(string name)
    where TState : class, ITableEntity, new()
    {
        TableProjectionStoreOptions options = new();
        var connectionString = TestContainer.GetConnectionString();
        return new TableProjectionStore<TState>(
            new TableClient(connectionString, options.ComposeProjectionTableName<TState>(name)),
            options.JsonSerializerOptions);
    }

}

public class ProjectionTrackingTests(ProjectionTrackingFixture fixture) : IClassFixture<ProjectionTrackingFixture>
{
    public const string StreamNamespace = "Fubar";
    
    [Fact]
    public async Task StampsTrackingPositionAfterEachApply()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(StreamNamespace, ct);
        var streamId = new StreamId("tracked-order-1");

        await store.AppendEventsAsync(streamId, StreamPosition.Start,
            [new OrderCaptured("tracked-1", new OrderAddress("1 Way", "Town", "00001"), [new OrderItem("Widget", 1, 9.99m)]),
             new OrderShipped("tracked-1", "TRACKED-001")], cancellationToken: ct);

        var subscription = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(ProjectionTrackingFixture.SubscriptionId, StreamNamespace, cancellationToken: ct);
        await subscription.PollAsync(ct);

        var projectionStore = fixture.CreateProjectionStore<TrackedOrderProjection>(StreamNamespace);
        var projection = await projectionStore.ReadAsync(new TrackedOrderProjection { OrderId = "tracked-1" }, ct);
        Assert.NotNull(projection);
        Assert.Equal("TRACKED-001", projection.Tracking);
        Assert.True(projection.ProjectionTrackingPosition > 0,
            "ProjectionTrackingPosition should be stamped with the last applied event's GlobalPosition.");
    }

    [Fact]
    public async Task SkipsAlreadyAppliedEventOnReplay()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(StreamNamespace, ct);
        var streamId = new StreamId("tracked-order-2");

        await store.AppendEventsAsync(streamId, StreamPosition.Start,
            [new OrderCaptured("tracked-2", new OrderAddress("2 Way", "Town", "00002"), [new OrderItem("Widget", 1, 9.99m)])],
            cancellationToken: ct);

        var subscription = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(ProjectionTrackingFixture.SubscriptionId, StreamNamespace, cancellationToken: ct);
        await subscription.PollAsync(ct);

        var projectionStore = fixture.CreateProjectionStore<TrackedOrderProjection>(StreamNamespace);
        var initial = await projectionStore.ReadAsync(new TrackedOrderProjection { OrderId = "tracked-2" }, ct);
        Assert.NotNull(initial);
        var positionAfterInitial = initial.ProjectionTrackingPosition;

        // Now append an OrderItemsAdded but simulate a re-delivery of a stale-position update by
        // bumping ProjectionTrackingPosition past the next event. The handler must short-circuit
        // and leave Items unchanged.
        initial.ProjectionTrackingPosition = long.MaxValue;
        await projectionStore.WriteAsync(initial, ct);

        await store.AppendEventsAsync(streamId, StreamPosition.Any,
            [new OrderItemsAdded("tracked-2", [new OrderItem("Should-Be-Ignored", 99, 99m)])],
            cancellationToken: ct);
        await subscription.PollAsync(ct);

        var afterReplay = await projectionStore.ReadAsync(new TrackedOrderProjection { OrderId = "tracked-2" }, ct);
        Assert.NotNull(afterReplay);
        Assert.DoesNotContain(afterReplay.Items ?? [], i => i.Name == "Should-Be-Ignored");
        Assert.Equal(long.MaxValue, afterReplay.ProjectionTrackingPosition);
    }
}

public class TrackedOrderProjection : ITableEntity, IProjectionTrackable
{
    public string PartitionKey { get; set; } = "";
    public string RowKey { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string OrderId { get; set; } = default!;
    public string? Tracking { get; set; }
    public List<OrderItem>? Items { get; set; }

    public long ProjectionTrackingPosition { get; set; }
}

public class TrackedOrderProjector :
    IEventInitialProjector<OrderCaptured, TrackedOrderProjection>,
    IEventProjector<OrderShipped, TrackedOrderProjection>,
    IEventProjector<OrderItemsAdded, TrackedOrderProjection>
{
    public TrackedOrderProjection Project(EventHandlerContext<OrderCaptured> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id, Items = [.. @event.Payload.Items] };

    public TrackedOrderProjection GetKey(EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<TrackedOrderProjection> ProjectAsync(TrackedOrderProjection state, EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default)
    {
        state.Tracking = @event.Payload.Tracking;
        return ValueTask.FromResult(state);
    }

    public TrackedOrderProjection GetKey(EventHandlerContext<OrderItemsAdded> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<TrackedOrderProjection> ProjectAsync(TrackedOrderProjection state, EventHandlerContext<OrderItemsAdded> @event, CancellationToken cancellationToken = default)
    {
        state.Items ??= [];
        state.Items.AddRange(@event.Payload.Items);
        return ValueTask.FromResult(state);
    }
}
