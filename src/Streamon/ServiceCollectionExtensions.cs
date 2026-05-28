using Microsoft.Extensions.DependencyInjection;

namespace Streamon;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Streamon core services, including the stream store provisioner and stream type provider
    /// </summary>
    /// <param name="services">The service collection to add the Streamon services to.</param>
    /// <returns>A <see cref="StreamBuilder"/> for further configuration.</returns>
    public static StreamBuilder AddStreamon(this IServiceCollection services) => new(services);
}
