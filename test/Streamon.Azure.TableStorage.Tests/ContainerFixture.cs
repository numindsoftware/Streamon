using Azure.Data.Tables;
using Streamon.Tests.Fixtures;
using System.Text.Json;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

public class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => 
        TestContainer = new AzuriteBuilder().WithName("streamon-azurite").WithPortBinding(10002, true).Build();

    public async Task DisposeAsync() => 
        await TestContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        await TestContainer.StartAsync();
        TableServiceClient = new TableServiceClient(TestContainer.GetConnectionString());
        var typeProvider = new StreamTypeProvider(new(JsonSerializerDefaults.Web));
        typeProvider.RegisterTypes<OrderCaptured>();
        TableStreamStoreProvisioner = new TableStreamStoreProvisioner(TableServiceClient!, new(typeProvider));
    }

    public AzuriteContainer TestContainer { get; private set; }

    public TableServiceClient TableServiceClient { get; private set; } = null!;

    public TableStreamStoreProvisioner TableStreamStoreProvisioner { get; private set; } = null!;
}
