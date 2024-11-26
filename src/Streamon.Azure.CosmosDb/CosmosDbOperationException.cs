namespace Streamon.Azure.CosmosDb;

[Serializable]
public class CosmosDbOperationException(string? message = default, Exception? innerException = default) : Exception(message, innerException) { }
