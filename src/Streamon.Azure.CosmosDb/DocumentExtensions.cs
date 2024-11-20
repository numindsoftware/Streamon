namespace Streamon.Azure.CosmosDb;

internal static class DocumentExtensions
{
    public static string ToStreamDocumentId(this StreamId streamId, EventId eventId, string separator = "/") => $"{streamId}{separator}{eventId}";
}
