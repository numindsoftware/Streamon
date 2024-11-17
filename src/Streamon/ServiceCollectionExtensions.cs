using Microsoft.Extensions.DependencyInjection;

namespace Streamon;

public static class ServiceCollectionExtensions
{
    public static StreamProvisionerBuilder AddStreamon(this IServiceCollection services)
    {
        return new StreamProvisionerBuilder(services).AddStreamTypeProvider();
    }
        
}
