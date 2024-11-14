using Microsoft.Extensions.DependencyInjection;
using Streamon.Memory;
using System.Text.Json;

namespace Streamon;

public class StreamProvisionerBuilder
{
    public StreamProvisionerBuilder(IServiceCollection services)
    {
        Services = services;
        services.AddTransient(sp => (ProvisionerBuilder ?? throw new InvalidOperationException("No store provisioner has been registered"))(sp));
    }

    public IServiceCollection Services { get; }
    public Func<IServiceProvider, IStreamStoreProvisioner>? ProvisionerBuilder { get; set; }

    public StreamProvisionerBuilder AddMemoryEventStore()
    {
        ProvisionerBuilder = sp => ActivatorUtilities.CreateInstance<MemoryStreamStoreProvisioner>(sp);
        return this;
    }

    public StreamProvisionerBuilder AddStreamTypeProvider<T>(JsonSerializerOptions? jsonSerializerOptions = default) where T : class, IStreamTypeProvider
    {
        if (jsonSerializerOptions is not null)
        {
            Services.AddSingleton<IStreamTypeProvider>(sp => new StreamTypeProvider(jsonSerializerOptions));
        }
        else
        {
            Services.AddSingleton<IStreamTypeProvider, StreamTypeProvider>();
        }
        return this;
    }
}
