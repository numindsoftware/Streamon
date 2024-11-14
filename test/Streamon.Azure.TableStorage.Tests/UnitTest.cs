using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage.Tests;

public class UnitTest
{
    [Fact]
    public async Task ProvisionsDefaultStreamStoreTable()
    {
        TableServiceClient tableServiceClient = new("UseDevelopmentStorage=true");
        StreamStorageProvisioner streamStorageProvisioner = new(tableServiceClient);
        
        TableStreamStore tableEventStore = await streamStorageProvisioner.CreateStoreAsync();
        Assert.NotNull(tableEventStore);
    }
}

