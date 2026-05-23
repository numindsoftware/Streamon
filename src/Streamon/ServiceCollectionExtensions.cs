using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Streamon;

public static class ServiceCollectionExtensions
{
    public static StreamProvisionerBuilder AddStreamon(this IServiceCollection services) =>
        new StreamProvisionerBuilder(services).AddStreamTypeProvider([Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly()]);
}
