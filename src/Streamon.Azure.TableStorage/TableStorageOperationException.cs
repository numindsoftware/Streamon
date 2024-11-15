namespace Streamon.Azure.TableStorage;

[Serializable]
public class TableStorageOperationException(string? message = default, Exception? innerException = default) : Exception(message, innerException) { }
