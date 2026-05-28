using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableProjectionStoreOptions
{
    /// <summary>
    /// Strategy used to compose the physical projection table name from
    /// <see cref="ProjectionTableNamingStrategy"/> and a provisioning-time suffix. Default: prefix + suffix.
    /// </summary>
    public Func<string, string, string> ProjectionTableNamingStrategy { get; set; } = NamingConventions.Concatenate;

    /// <summary>
    /// Composes the projection table name for the given suffix. When <paramref name="prefix"/> is null the
    /// <see cref="ProjectionTableNamingStrategy"/> is used.
    /// </summary>
    public string ComposeProjectionTableName<TState>(string? suffix)
    {
        
        var entityTableName = typeof(TState).GetCustomAttribute<TableAttribute>(true)?.Name ?? typeof(TState).Name;
        return ProjectionTableNamingStrategy(entityTableName, suffix ?? string.Empty);
    }

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
