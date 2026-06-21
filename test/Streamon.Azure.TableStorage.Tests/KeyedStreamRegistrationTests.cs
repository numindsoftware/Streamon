using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

/// <summary>
/// Functional tests proving the keyed registration of multiple named stream stores. Calling
/// <c>AddStreamon("name")</c> registers an <see cref="IStreamStoreProvisioner"/> as a keyed service
/// (keyed by the stream name) whose base table name is derived from that same name. The per-store
/// suffix passed to <see cref="IStreamStoreProvisioner.CreateStoreAsync"/> is appended to that base,
/// so <c>"orders"</c> + <c>"ABC"</c> resolves to the Azure table <c>ordersABC</c>.
/// </summary>
public class KeyedStreamRegistrationFixture : IAsyncLifetime
{
    public const string OrdersStream = "Orders";
    public const string ShipmentStream = "Shipment";

    public KeyedStreamRegistrationFixture() =>
        TestContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithName("streamon-azurite-keyed")
            .WithPortBinding(10002, true)
            .Build();

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync() => await TestContainer.DisposeAsync();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    public async ValueTask InitializeAsync()
    {
        await TestContainer.StartAsync();
        ConnectionString = TestContainer.GetConnectionString();

        var services = new ServiceCollection();

        // Two named streams registered side by side. Each AddStreamon(name) call registers a keyed
        // IStreamStoreProvisioner whose key — and base table name — is the supplied stream name.
        services.AddStreamon(OrdersStream)
            .UseTableStorageStreamStore(ConnectionString);

        services.AddStreamon(ShipmentStream)
            .UseTableStorageStreamStore(ConnectionString);

        ServiceProvider = services.BuildServiceProvider();
    }

    public AzuriteContainer TestContainer { get; }
    public string ConnectionString { get; private set; } = null!;
    public IServiceProvider ServiceProvider { get; private set; } = null!;
}

public class KeyedStreamRegistrationTests(KeyedStreamRegistrationFixture fixture) : IClassFixture<KeyedStreamRegistrationFixture>
{
    [Fact]
    public async Task ResolvesKeyedProvisionersAndComposesTableNamesWithoutSuffix()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.ServiceProvider.CreateScope();

        // Named stream stores are resolved by key — there is one provisioner per registered name.
        var ordersProvisioner = scope.ServiceProvider.GetRequiredKeyedService<IStreamStoreProvisioner>(KeyedStreamRegistrationFixture.OrdersStream);
        var shipmentProvisioner = scope.ServiceProvider.GetRequiredKeyedService<IStreamStoreProvisioner>(KeyedStreamRegistrationFixture.ShipmentStream);

        Assert.NotSame(ordersProvisioner, shipmentProvisioner);

        await ordersProvisioner.CreateStoreAsync(cancellationToken: ct);
        await shipmentProvisioner.CreateStoreAsync(cancellationToken: ct);

        var tables = await ListTableNamesAsync(ct);

        Assert.Contains("StreamonOrders", tables);
        Assert.Contains("StreamonShipment", tables);
    }

    [Fact]
    public async Task ComposesTableNamesFromStreamNameAndSuffix()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.ServiceProvider.CreateScope();

        var ordersProvisioner = scope.ServiceProvider.GetRequiredKeyedService<IStreamStoreProvisioner>(KeyedStreamRegistrationFixture.OrdersStream);
        var shipmentProvisioner = scope.ServiceProvider.GetRequiredKeyedService<IStreamStoreProvisioner>(KeyedStreamRegistrationFixture.ShipmentStream);

        // The suffix is appended to the stream name to compose the physical table name.
        await ordersProvisioner.CreateStoreAsync("ABC", ct);
        await ordersProvisioner.CreateStoreAsync("DEF", ct);
        await shipmentProvisioner.CreateStoreAsync("ABC", ct);
        await shipmentProvisioner.CreateStoreAsync("DEF", ct);

        var tables = await ListTableNamesAsync(ct);

        Assert.Contains("StreamonOrdersABC", tables);
        Assert.Contains("StreamonOrdersDEF", tables);
        Assert.Contains("StreamonShipmentABC", tables);
        Assert.Contains("StreamonShipmentDEF", tables);
    }

    [Fact]
    public void NamedStreamsAreRegisteredOnlyAsKeyedServices()
    {
        // Named streams are keyed-only: the default (non-keyed) provisioner must not resolve.
        Assert.Null(fixture.ServiceProvider.GetService<IStreamStoreProvisioner>());
    }

    [Fact]
    public async Task UnnamedStreamIsRegisteredAsNonKeyedService()
    {
        var ct = TestContext.Current.CancellationToken;

        // AddStreamon() configured WITHOUT a name registers a non-keyed IStreamStoreProvisioner
        // whose base table name comes from TableStreamStoreOptions.StreamTableName.
        const string defaultTableName = "defaultnokey";
        var services = new ServiceCollection();
        services.AddStreamon()
            .UseTableStorageStreamStore(fixture.ConnectionString, options =>
            {
                options.StreamTableName = defaultTableName;
                options.StreamTypeProvider = new StreamTypeProvider();
            });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // The provisioner resolves without a key and produces the default-named table.
        var provisioner = scope.ServiceProvider.GetRequiredService<IStreamStoreProvisioner>();
        await provisioner.CreateStoreAsync(cancellationToken: ct);

        var tables = await ListTableNamesAsync(ct);

        Assert.Contains(defaultTableName, tables);
    }

    private async Task<List<string>> ListTableNamesAsync(CancellationToken cancellationToken)
    {
        var tableServiceClient = new global::Azure.Data.Tables.TableServiceClient(fixture.ConnectionString);
        var names = new List<string>();
        await foreach (var table in tableServiceClient.QueryAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            names.Add(table.Name);
        }
        return names;
    }
}
