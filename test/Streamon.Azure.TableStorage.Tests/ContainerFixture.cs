using Azure.Data.Tables;
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
    }

    public AzuriteContainer TestContainer { get; private set; }

    public TableServiceClient? TableServiceClient { get; private set; }
}
