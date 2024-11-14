﻿using Azure;
using System.Net;

namespace Streamon.Azure.TableStorage;

internal static class TableClientExtensions
{
    public static void ThrowOnError(this Response response, string? optionalFailureMessage = default)
    {
        if (response.Status >= (int)HttpStatusCode.BadRequest) throw new TableStorageOperationException(optionalFailureMessage);
    }
}
