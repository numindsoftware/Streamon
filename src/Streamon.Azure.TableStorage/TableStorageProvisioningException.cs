namespace Streamon.Azure.TableStorage;

[Serializable]
public class TableStorageProvisioningException(string? message = default) : Exception(message) { }
