using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Streamon.Memory;

namespace Streamon;

/// <summary>
/// Fluent builder used by <see cref="ServiceCollectionExtensions.AddStreamon"/> to register the
/// stream store provisioner, stream type provider, and core <see cref="StreamOptions"/> defaults
/// (including the cross-cutting stream naming strategy).
/// </summary>
public class StreamBuilder
{
#pragma warning disable IDE0290 // Use primary constructor, disabled to avoid introducing local private field when a property already exists for the same purpose, avoid double variable declaration and potential confusion
    public StreamBuilder(IServiceCollection services, string? name = default) => (Services, Name) = (services, name);
#pragma warning restore IDE0290 // Use primary constructor
    public IServiceCollection Services { get; }
    public string? Name { get; }

    public StreamBuilder AddMemoryEventStore()
    {
        if (string.IsNullOrWhiteSpace(Name)) Services.TryAddSingleton<IStreamStoreProvisioner, MemoryStreamStoreProvisioner>();
        else Services.TryAddKeyedScoped<IStreamStoreProvisioner, MemoryStreamStoreProvisioner>(Name);
        return this;
    }
}
