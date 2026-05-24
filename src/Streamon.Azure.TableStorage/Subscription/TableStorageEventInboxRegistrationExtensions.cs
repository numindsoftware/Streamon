using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class TableStorageEventInboxRegistrationExtensions
{
    /// <summary>
    /// Registers a <see cref="TableStorageEventInbox"/> as the application-wide
    /// <see cref="IEventInbox"/> singleton.
    /// </summary>
    public static IServiceCollection AddTableStorageEventInbox(
        this IServiceCollection services,
        string connectionString,
        string tableName = TableStorageEventInbox.DefaultInboxTableName) =>
        services.AddSingleton<IEventInbox>(_ =>
            new TableStorageEventInbox(new TableClient(connectionString, tableName)));
}