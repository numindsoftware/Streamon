using Microsoft.Extensions.DependencyInjection;

namespace Streamon;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Streamon core services, including the stream store provisioner and stream type provider
    /// </summary>
    /// <param name="services">The service collection to add the Streamon services to.</param>
    /// <param name="name">The name to use for the Streamon services. Will also serve as the base name of the stream store.</param>
    /// <returns>A <see cref="StreamBuilder"/> for further configuration.</returns>
    public static StreamBuilder AddStreamon(this IServiceCollection services, string? name = default) => new(services, name);
}
