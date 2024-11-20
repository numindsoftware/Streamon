using Microsoft.Azure.Cosmos;
using System.Net;

namespace Streamon.Azure.CosmosDb;

internal static class CosmosDbResponseExtensions
{
    public static void ThrowOnError<T>(this Response<T> response, string? optionalFailureMessage = default)
    {
        if (response.StatusCode >= HttpStatusCode.BadRequest) throw new CosmosDbOperationException(optionalFailureMessage);
    }

    public static void ThrowOnError(this ResponseMessage response, string? optionalFailureMessage = default)
    {
        if (!response.IsSuccessStatusCode) throw new CosmosDbOperationException(optionalFailureMessage);
    }
}
