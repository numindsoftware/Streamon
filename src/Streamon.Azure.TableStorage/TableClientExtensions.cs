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

    public static ValueTask<bool> CheckTableExistsAsync(this TableServiceClient tableClient, string name, CancellationToken cancellationToken = default) =>
        tableClient.QueryAsync(t => string.Compare(t.Name, name) == 0, 1, cancellationToken).GetAsyncEnumerator(cancellationToken).MoveNextAsync();
}
