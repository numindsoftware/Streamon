namespace Streamon;

public class EventMetadata : Dictionary<string, string>
{
    public EventMetadata() { }

    public EventMetadata(IEnumerable<KeyValuePair<string, string>> keyValuePairs) : base(keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value)) { }

    public EventMetadata(params KeyValuePair<string, string>[] keyValuePairs) : this(keyValuePairs.AsEnumerable()) { }

    public void AddRange(params KeyValuePair<string, string>[] keyValuePairs) => AddRange(keyValuePairs.AsEnumerable());

    public void AddRange(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
    {
        foreach (var kv in keyValuePairs) Add(kv.Key, kv.Value);
    }

    public override string ToString() => string.Join(Environment.NewLine, this.Select(kv => $"{kv.Key}: {kv.Value}"));

    public string GetMetadataValue(string key) => GetMetadataValue(key, s => s);

    public T GetMetadataValue<T>(string key, Func<string, T> converter)
    {
        if (!TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException(key);
        }

        try
        {
            return converter(value);
        }
        catch (Exception e)
        {
            throw new FormatException($"Value {value} could not be converted to type {typeof(T).Name}", e);
        }
    }

    public string CorrelationId
    {
        get => (TryGetValue(nameof(CorrelationId), out string? value) ? value : null) ?? string.Empty;
        set => this[nameof(CorrelationId)] = value;
    }

    public string CausationId
    {
        get => (TryGetValue(nameof(CausationId), out string? value) ? value : null) ?? string.Empty;
        set => this[nameof(CausationId)] = value;
    }
}
