using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Azure.CosmosDb;

public static class StreamProvisionerBuilderExtensions
{
    public static StreamProvisionerBuilder AddCosmosDbStreamStore(this StreamProvisionerBuilder builder, CosmosClient cosmosClient, CosmosDbStreamStoreOptions options)
    {
        builder.ProvisionerBuilder = sp =>
        {
            return ActivatorUtilities.CreateInstance<CosmosDbStreamStoreProvisioner>(sp, cosmosClient, options);
        };
        return builder;
    }

    public static StreamProvisionerBuilder AddCosmosDbStreamStore(this StreamProvisionerBuilder builder, string connectionString, CosmosDbStreamStoreOptions options)
        => builder.AddCosmosDbStreamStore(new CosmosClient(connectionString), options);
}
