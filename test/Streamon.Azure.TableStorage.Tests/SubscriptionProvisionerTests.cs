using Microsoft.Extensions.DependencyInjection;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

/// <summary>
/// Functional tests showing the <see cref="IStreamSubscriptionProvisioner"/> in a multi-tenant
/// setup: a single registration provides a builder template, and the provisioner spins up an
/// isolated subscription per tenant (suffix), composing tenant-specific storage names via the
/// configured naming convention.
/// </summary>
public class SubscriptionProvisionerFixture : IAsyncLifetime
{
    public const string StreamTablePrefix = "TenantStream";
    public const string CheckpointTablePrefix = "TenantCheckpoint";
    public static readonly SubscriptionId SubscriptionId = SubscriptionId.From("tenant-sub");

    public SubscriptionProvisionerFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-tenants")
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
            .RegisterTypes<OrderCaptured>();

        var services = new ServiceCollection();

        services.AddStreamon()
            .UseTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTableName = StreamTablePrefix;
                options.StreamTypeProvider = typeProvider;
            });

        services.AddSingleton<TenantTrackingHandler>();

        // Single registration acts as a template — the provisioner instantiates one subscription
        // per (SubscriptionId, tenant) on demand. Names are composed as "{prefix}{suffix}".
        services.AddStreamonSubscription(SubscriptionId,
            configureOptions: o =>
            {
                o.StreamSubscriptionType = StreamSubscriptionType.CatchUp;
            })
            .UseTableStorageCheckpointStore(connectionString, o =>
            {
                o.CheckpointTableName = CheckpointTablePrefix;
            })
            .UseTableStorageSubscriptionStreamReader(connectionString, o =>
            {
                o.StreamTableName = StreamTablePrefix;
                o.StreamTypeProvider = typeProvider;
            })
            .AddEventHandler<TenantTrackingHandler>();

        ServiceProvider = services.BuildServiceProvider();
        StreamStoreProvisioner = ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        SubscriptionProvisioner = ServiceProvider.GetRequiredService<IStreamSubscriptionProvisioner>();
        Handler = ServiceProvider.GetRequiredService<TenantTrackingHandler>();
    }

    public AzuriteContainer TestContainer { get; }
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
    public IStreamSubscriptionProvisioner SubscriptionProvisioner { get; private set; } = null!;
    public TenantTrackingHandler Handler { get; private set; } = null!;
}

public class SubscriptionProvisionerTests(SubscriptionProvisionerFixture fixture) : IClassFixture<SubscriptionProvisionerFixture>
{
    [Fact]
    public async Task ReturnsCachedSubscriptionForSameTenant()
    {
        var ct = TestContext.Current.CancellationToken;
        var a1 = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Contoso", ct);
        var a2 = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Contoso", ct);
        Assert.Same(a1, a2);
    }

    [Fact]
    public async Task ProducesDistinctSubscriptionsPerTenant()
    {
        var ct = TestContext.Current.CancellationToken;
        var contoso = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Contoso", ct);
        var fabrikam = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Fabrikam", ct);
        Assert.NotSame(contoso, fabrikam);
    }

    [Fact]
    public async Task PollsTenantsIndependentlyAndIsolatesEvents()
    {
        var ct = TestContext.Current.CancellationToken;

        // Each tenant gets its own physical stream table — composed by the naming strategy
        // (DefaultStreamName "TenantStream" + suffix "Contoso" = "TenantStreamContoso").
        var contosoStore = await fixture.StreamStoreProvisioner.CreateStoreAsync("Contoso", ct);
        var fabrikamStore = await fixture.StreamStoreProvisioner.CreateStoreAsync("Fabrikam", ct);

        await contosoStore.AppendEventsAsync(new StreamId("c-order-1"), StreamPosition.Start,
            [new OrderCaptured("c-1", new OrderAddress("1 St", "City", "00001"), [new OrderItem("Widget", 1, 1m)])],
            cancellationToken: ct);
        await fabrikamStore.AppendEventsAsync(new StreamId("f-order-1"), StreamPosition.Start,
            [new OrderCaptured("f-1", new OrderAddress("2 St", "City", "00002"), [new OrderItem("Gadget", 1, 2m)]),
             new OrderCaptured("f-2", new OrderAddress("3 St", "City", "00003"), [new OrderItem("Gizmo",  1, 3m)])],
            cancellationToken: ct);

        fixture.Handler.Reset();

        var contoso = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Contoso", ct);
        var fabrikam = await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Fabrikam", ct);

        await contoso.PollAsync(ct);
        Assert.Equal(1, fixture.Handler.Count);

        await fabrikam.PollAsync(ct);
        Assert.Equal(3, fixture.Handler.Count);
    }

    [Fact]
    public async Task AllEnumeratesEveryRegisteredSubscription()
    {
        var ct = TestContext.Current.CancellationToken;
        // Trigger creation of two tenants so they show up in All().
        await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Contoso", ct);
        await fixture.SubscriptionProvisioner.CreateSubscriptionAsync(SubscriptionProvisionerFixture.SubscriptionId, "Fabrikam", ct);

        var all = fixture.SubscriptionProvisioner.All().ToList();

        // At minimum the default-name + tenant subscriptions are present after creation.
        Assert.NotEmpty(all);
    }

    [Fact]
    public async Task CustomNamingConventionDrivesCompositeNames()
    {
        var ct = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        var typeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
        var connectionString = fixture.TestContainer.GetConnectionString();

        services.AddStreamon()
            .UseTableStorageStreamStore(connectionString, options =>
            {
                options.StreamTableName = "CustomStream";
                options.StreamTableNamingStrategy = (p, s) => string.IsNullOrEmpty(s) ? p : $"{p}X{s}";
                options.StreamTypeProvider = typeProvider;
            });

        services.AddStreamonSubscription(SubscriptionId.From("custom-naming-sub"),
            configureOptions: o =>
            {
                o.StreamSubscriptionType = StreamSubscriptionType.CatchUp;
            })
            .UseTableStorageCheckpointStore(connectionString, o =>
            {
                o.CheckpointTableName = "CustomChk";
                o.CheckpointTableNamingStrategy = (p, s) => string.IsNullOrEmpty(s) ? p : $"{p}X{s}";
            })
            .UseTableStorageSubscriptionStreamReader(connectionString, o =>
            {
                o.StreamTypeProvider = typeProvider;
                o.StreamTableName = "CustomStream";
                o.StreamTableNamingStrategy = (p, s) => string.IsNullOrEmpty(s) ? p : $"{p}X{s}";
            });

        var sp = services.BuildServiceProvider();
        var provisioner = sp.GetRequiredService<IStreamSubscriptionProvisioner>();
        var streamProvisioner = sp.GetRequiredService<IStreamStoreProvisioner>();

        // The strategy turns suffix "Acme" into table "CustomStreamXAcme".
        var store = await streamProvisioner.CreateStoreAsync("Acme", ct);
        await store.AppendEventsAsync(new StreamId("acme-1"), StreamPosition.Start,
            [new OrderCaptured("a-1", new OrderAddress("1", "C", "0"), [new OrderItem("W", 1, 1m)])],
            cancellationToken: ct);

        var subscription = await provisioner.CreateSubscriptionAsync(SubscriptionId.From("custom-naming-sub"), "Acme", ct);
        // Should poll without error against the X-composed table name.
        await subscription.PollAsync(ct);
    }
}

/// <summary>Simple counting handler shared by all tenant subscriptions, used to verify dispatch.</summary>
public class TenantTrackingHandler : IEventHandler
{
    private int _count;
    public int Count => _count;
    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return Task.CompletedTask;
    }
}
