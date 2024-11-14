namespace Streamon.Azure.TableStorage;

[Serializable]
public class TableStorageOperationException(string? message = default) : Exception(message) { }
