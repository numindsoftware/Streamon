namespace Streamon.Azure.TableStorage;

public record EventStoreOptions(string EntityFieldPrefix = "_ef_", string MetadataFieldPrefix = "_mt_");
