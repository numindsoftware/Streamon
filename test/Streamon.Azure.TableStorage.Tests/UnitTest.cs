using Azure.Data.Tables;
using System.Text.Json;

namespace Streamon.Azure.TableStorage.Tests;

public class UnitTest
{
    [Fact]
    public async Task ProvisionsDefaultStreamStoreTable()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        TableServiceClient tableServiceClient = new("UseDevelopmentStorage=true");
        StreamStorageProvisioner streamStorageProvisioner = new(tableServiceClient, new StreamTypeProvider(serializerOptions));
        
        TableStreamStore tableEventStore = await streamStorageProvisioner.CreateStoreAsync();
        Assert.NotNull(tableEventStore);
    }
}

