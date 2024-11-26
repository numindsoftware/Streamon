using Azure;
using Azure.Data.Tables;
using System.Net;

namespace Streamon.Azure.TableStorage;

internal static class TableClientExtensions
{
    public static Response ThrowOnError(this Response response, string? optionalFailureMessage = default)
    {
        if (response.Status >= (int)HttpStatusCode.BadRequest) throw new TableStorageOperationException(optionalFailureMessage);
        return response;
    }

    public static async Task<bool> CheckTableExistsAsync(this TableServiceClient tableClient, string name, CancellationToken cancellationToken = default) =>
        await tableClient.QueryAsync(name, 1, cancellationToken).GetAsyncEnumerator(cancellationToken).MoveNextAsync();
}
