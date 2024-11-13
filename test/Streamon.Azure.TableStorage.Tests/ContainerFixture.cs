using Azure.Data.Tables;
using Testcontainers.Azurite;

namespace Streamon.Azure.TableStorage.Tests;

internal class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture()
    {
        TestContainer = new AzuriteBuilder().WithPortBinding(10002, true).Build();
        TableServiceClient = new TableServiceClient(TestContainer.GetTableEndpoint());
    }

    public async Task DisposeAsync() => await TestContainer.DisposeAsync();

    public async Task InitializeAsync() => await TestContainer.StartAsync();

    public AzuriteContainer TestContainer { get; private set; }

    public TableServiceClient TableServiceClient { get; private set; }
}
