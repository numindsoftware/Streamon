using System.Text.Json;

namespace Streamon.Azure.TableStorage.Tests;

public class UnitTest(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    [Fact]
    public async Task ProvisionsDefaultStreamStoreTable()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        TableStreamStoreProvisioner streamStorageProvisioner = new(containerFixture.TableServiceClient!, new StreamTypeProvider(serializerOptions));
        
        var tableEventStore = await streamStorageProvisioner.CreateStoreAsync();
        Assert.NotNull(tableEventStore);
    }
}

