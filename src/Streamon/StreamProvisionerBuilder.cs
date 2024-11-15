using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streamon.Memory;
using System.Reflection;
using System.Text.Json;

namespace Streamon;

public class StreamProvisionerBuilder
{
    public StreamProvisionerBuilder(IServiceCollection services)
    {
        Services = services;
        services.AddTransient(sp => (ProvisionerBuilder ?? throw new InvalidOperationException("No store provisioner has been registered"))(sp));
        services.AddSingleton(sp => (StreamTypeProviderBuilder ?? throw new InvalidOperationException("No stream type provider has been registered"))(sp));
    }

    public IServiceCollection Services { get; }
    public Func<IServiceProvider, IStreamStoreProvisioner>? ProvisionerBuilder { get; set; }
    public Func<IServiceProvider, IStreamTypeProvider>? StreamTypeProviderBuilder { get; set; }

    public StreamProvisionerBuilder AddMemoryEventStore()
    {
        ProvisionerBuilder = static sp => ActivatorUtilities.CreateInstance<MemoryStreamStoreProvisioner>(sp);
        return this;
    }

    public StreamProvisionerBuilder AddStreamTypeProvider(IEnumerable<Assembly>? assemblies = default, JsonSerializerOptions? jsonSerializerOptions = default)
    {
        StreamTypeProviderBuilder = sp =>
        {
            jsonSerializerOptions ??= sp.GetService<IOptions<JsonSerializerOptions>>()?.Value ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            StreamTypeProvider provider = new(jsonSerializerOptions);
            provider.RegisterTypes(assemblies ?? []);
            return provider;
        };
        return this;
    }

    public StreamProvisionerBuilder AddStreamTypeProvider<T>(JsonSerializerOptions? jsonSerializerOptions = default) where T : class, IStreamTypeProvider
    {
        StreamTypeProviderBuilder = sp => ActivatorUtilities.CreateInstance<T>(sp, jsonSerializerOptions is null ? [] : [jsonSerializerOptions]);
        return this;
    }
}
