using System.Reflection;
using System.Text.Json;

namespace Streamon;

public class StreamTypeProvider : IStreamTypeProvider
{

    private readonly List<Assembly> _registeredAssemblies = [];
    private readonly Dictionary<string, Type> _eventTypesRegistry = [];
    private readonly JsonSerializerOptions _serializerOptions;

    public StreamTypeProvider(JsonSerializerOptions? serializerOptions = default)
    {
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        RegisterTypes(Assembly.GetEntryAssembly()!);
        RegisterTypes(Assembly.GetExecutingAssembly());
        RegisterTypes(Assembly.GetCallingAssembly());
    }

    public IStreamTypeProvider RegisterTypes(Assembly assembly)
    {
        if (!_registeredAssemblies.Contains(assembly))
        {
            _registeredAssemblies.Add(assembly);
            assembly.GetTypes()
                .Select(static t => new { Type = t, Attribute = t.GetCustomAttribute<EventTypeAttribute>() })
                .Where(static t => t.Attribute?.Name is not null)
                .ToList()
                .ForEach(t => RegisterType(t.Attribute!.Name, t.Type));
        }
        return this;
    }

    public StreamTypeProvider RegisterType(string name, Type eventType)
    {
        if (!_eventTypesRegistry.TryAdd(name, eventType)) _eventTypesRegistry[name] = eventType;
        return this;
    }

    public object ResolveEvent(string name, string data)
    {
        if (!_eventTypesRegistry.TryGetValue(name, out var eventType))
        {
            var type = _registeredAssemblies
                .SelectMany(static a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsEnum && !t.IsInterface)
                .Select(static t => new { Type = t, Attribute = t.GetCustomAttribute<EventTypeAttribute>() })
                .Where(t => (t.Attribute?.Name ?? t.Type.Name) == name)
                .SingleOrDefault() ?? throw new EventTypeNotFoundException(name);
            _eventTypesRegistry[name] = eventType = type.Type;
        }
        return JsonSerializer.Deserialize(data, eventType, _serializerOptions) ?? throw new StreamTypeProviderException(name, eventType, $"The event data couldn't be deserialized to type {eventType}");
    }

    public EventTypeInfo SerializeEvent(object @event)
    {
        var eventType = @event.GetType();
        var eventTypeName = eventType.GetCustomAttribute<EventTypeAttribute>()?.Name ?? @event.GetType().Name;
        RegisterType(eventTypeName, eventType);
        var eventData = JsonSerializer.Serialize(@event, _serializerOptions) ?? throw new StreamTypeProviderException(eventTypeName, eventType, $"The event object counldn't be serialized from type {eventType}");
        return new(eventTypeName, eventData);
    }

    public EventMetadata? ResolveMetadata(string? data) =>
        string.IsNullOrWhiteSpace(data) ? null : JsonSerializer.Deserialize<EventMetadata>(data, _serializerOptions);

    public string? SerializeMetadata(EventMetadata? metadata) =>
        metadata is not null ? JsonSerializer.Serialize(metadata, _serializerOptions) ?? throw new StreamTypeProviderException("metadata", typeof(EventMetadata), "The metadata object couldn't be serialized") : default;
}
