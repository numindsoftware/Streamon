using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

/// <summary>
/// Functional tests proving the <see cref="IEventInbox"/> contract and showing how to wire it
/// from DI: any handler can wrap its side effect with <see cref="EventInboxExtensions.RunOnceAsync"/>
/// to get idempotency under at-least-once delivery.
/// </summary>
public class EventInboxFixture : IAsyncLifetime
{
    public const string StreamTableName = nameof(EventInboxTests);
    public static readonly SubscriptionId SubscriptionId = SubscriptionId.From("inbox-sub");

    public EventInboxFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-inbox")
            .WithPortBinding(10002, true)
            .Build();

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync() => await TestContainer.DisposeAsync();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    public async ValueTask InitializeAsync()
    {
        await TestContainer.StartAsync();
        var connectionString = TestContainer.GetConnectionString();

        var typeProvider = new StreamTypeProvider()
            .RegisterTypes<OrderCaptured>()
            .RegisterTypes<OrderShipped>();

        var services = new ServiceCollection();

        services.AddStreamon()
            .UseTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTableName = StreamTableName;
                options.StreamTypeProvider = typeProvider;
            });

        // Register the counting handler so it can be resolved by the subscription DI bridge.
        services.AddSingleton<CountingInboxHandler>();

        services.AddStreamonSubscription(SubscriptionId)
            .UseTableStorageCheckpointStore(connectionString)
            .UseTableStorageSubscriptionStreamReader(connectionString,
                options =>
                {
                    options.StreamTableName = StreamTableName;
                    options.StreamTypeProvider = typeProvider;
                })
            .UseTableStorageEventInbox(connectionString)
            .UseInboxDeduplication()
            .AddEventHandler<CountingInboxHandler>();

        ServiceProvider = services.BuildServiceProvider();
        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionProvisioner = ServiceProvider.GetRequiredService<IStreamSubscriptionProvisioner>();
        Handler = ServiceProvider.GetRequiredService<CountingInboxHandler>();
    }

    public AzuriteContainer TestContainer { get; }
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public IStreamSubscriptionProvisioner SubscriptionProvisioner { get; private set; } = null!;
    public CountingInboxHandler Handler { get; private set; } = null!;
}

public class EventInboxTests(EventInboxFixture fixture) : IClassFixture<EventInboxFixture>
{
    private const string StreamNamespace = "Mac";

    [Fact]
    public async Task RunOnceAsyncExecutesSideEffectOnlyForFirstDelivery()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(StreamNamespace, ct);
        var streamId = new StreamId("inbox-order-1");

        await store.AppendEventsAsync(streamId, StreamPosition.Start,
            [new OrderCaptured("inbox-1", new OrderAddress("1 St", "City", "00001"), [new OrderItem("Widget", 1, 1m)])],
            cancellationToken: ct);

        var subscription = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(EventInboxFixture.SubscriptionId, StreamNamespace, cancellationToken: ct);

        fixture.Handler.Reset();

        await subscription.PollAsync(ct);
        var firstCount = fixture.Handler.SideEffectCount;

        // Force redelivery of the already-processed events by rewinding the checkpoint to Start.
        // The inbox should suppress the side effect on the second pass.
        await RewindCheckpointAsync(ct);
        await subscription.PollAsync(ct);

        Assert.Equal(1, firstCount);
        Assert.Equal(1, fixture.Handler.SideEffectCount);
    }

    [Fact]
    public async Task InboxScopesAreIsolatedPerConsumer()
    {
        var inboxTableName = new TableEventInboxStoreOptions().DefaultInboxName;
        var ct = TestContext.Current.CancellationToken;
        var inbox = new TableEventInboxStore(
            new global::Azure.Data.Tables.TableServiceClient(fixture.TestContainer.GetConnectionString()), inboxTableName);

        var eventId = EventId.New();
        var @event = new Event(
            new StreamId("scope-stream"),
            eventId,
            StreamPosition.From(1),
            StreamPosition.From(1),
            DateTimeOffset.UtcNow,
            BatchId.New(),
            OrderEvents.OrderCaptured);

        await inbox.MarkProcessedAsync(SubscriptionId.From("sub-A"), "consumer-1", @event, ct);

        Assert.True(await inbox.HasProcessedAsync(SubscriptionId.From("sub-A"), "consumer-1", eventId, ct));
        Assert.False(await inbox.HasProcessedAsync(SubscriptionId.From("sub-A"), "consumer-2", eventId, ct));
        Assert.False(await inbox.HasProcessedAsync(SubscriptionId.From("sub-B"), "consumer-1", eventId, ct));
    }

    [Fact]
    public async Task MarkProcessedAsyncIsIdempotent()
    {
        var inboxTableName = new TableEventInboxStoreOptions().DefaultInboxName;
        var ct = TestContext.Current.CancellationToken;
        var inbox = new TableEventInboxStore(
            new global::Azure.Data.Tables.TableServiceClient(fixture.TestContainer.GetConnectionString()), inboxTableName);

        var @event = new Event(
            new StreamId("idem-stream"),
            EventId.New(),
            StreamPosition.From(2),
            StreamPosition.From(2),
            DateTimeOffset.UtcNow,
            BatchId.New(),
            OrderEvents.OrderShipped);

        await inbox.MarkProcessedAsync(SubscriptionId.From("sub-idem"), "c", @event, ct);
        await inbox.MarkProcessedAsync(SubscriptionId.From("sub-idem"), "c", @event, ct); // must not throw
    }

    private async Task RewindCheckpointAsync(CancellationToken ct)
    {
        var checkpointTableName = new TableCheckpointStoreOptions().CheckpointTableName;
        var checkpointStore = new TableCheckpointStore(
            new global::Azure.Data.Tables.TableServiceClient(fixture.TestContainer.GetConnectionString()),
            checkpointTableName);
        await checkpointStore.SetCheckpointAsync(EventInboxFixture.SubscriptionId, StreamPosition.Start, ct);
    }
}

/// <summary>
/// Sample handler that delegates idempotency to <see cref="IEventInbox"/>; the protected side
/// effect (an in-memory counter increment) is what most real handlers replace with an external call.
/// </summary>
public class CountingInboxHandler() : IEventHandler
{
    private int _count;
    public int SideEffectCount => _count;
    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count); 
        return Task.CompletedTask;
    }
}
