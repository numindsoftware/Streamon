namespace Streamon.Azure.CosmosDb;

public class CosmosDbOperationException(string? message = default, Exception? innerException = default) : Exception(message, innerException) { }
