using System.Reflection;

namespace Streamon;

public static class StreamTypeProviderExtensions
{
    public static IStreamTypeProvider RegisterTypes(this IStreamTypeProvider provider, IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies) provider.RegisterTypes(assembly);
        return provider;
    }

    public static IStreamTypeProvider RegisterTypes(this IStreamTypeProvider provider, params Type[] types) =>
        provider.RegisterTypes(types.Select(static t => t.Assembly));

    public static IStreamTypeProvider RegisterTypes<T>(this IStreamTypeProvider provider) => 
        provider.RegisterTypes([typeof(T).Assembly]);
}
