using System.Reflection;
using System.Text.Json;

namespace Streamon.Tests;

public class StreamTypeProviderTests
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private readonly StreamTypeProvider _provider;

    public StreamTypeProviderTests()
    {
        _provider = new StreamTypeProvider(_serializerOptions);
    }

    [Fact]
    public void RegisterType_ShouldRegisterType()
    {
        _provider.RegisterType<TestEvent>("TestEvent");
        var result = _provider.ResolveEvent("TestEvent", "{}");
        Assert.IsType<TestEvent>(result);
    }

    [Fact]
    public void RegisterTypes_ShouldRegisterTypesFromAssembly()
    {
        _provider.RegisterTypes([Assembly.GetExecutingAssembly()]);
        var result = _provider.ResolveEvent("TestEvent", "{}");
        Assert.IsType<TestEvent>(result);
    }

    [Fact]
    public void ResolveEvent_ShouldThrowExceptionForUnknownEvent()
    {
        Assert.Throws<EventTypeNotFoundException>(() => _provider.ResolveEvent("UnknownEvent", "{}"));
    }

    [Fact]
    public void ResolveEvent_ShouldResolveByAttribute()
    {
        _provider.RegisterTypes([Assembly.GetExecutingAssembly()]);
        var result = _provider.ResolveEvent("v1.test_event2", "{}");
        Assert.NotNull(result);
    }

    [Fact]
    public void SerializeEvent_ShouldReturnEventTypeInfo()
    {
        var testEvent = new TestEvent();
        var result = _provider.SerializeEvent(testEvent);
        Assert.Equal("TestEvent", result.Type);
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
    }

    [Fact]
    public void ResolveMetadata_ShouldReturnMetadata()
    {
        var metadata = new EventMetadata { { "key", "value" } };
        var data = JsonSerializer.Serialize(metadata, _serializerOptions);
        var result = _provider.ResolveMetadata(data);
        Assert.NotNull(result);
        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void SerializeMetadata_ShouldReturnSerializedMetadata()
    {
        var metadata = new EventMetadata { { "key", "value" } };
        var result = _provider.SerializeMetadata(metadata);
        Assert.NotNull(result);
        Assert.Contains("\"key\":\"value\"", result);
    }

    private class TestEvent { }

    [EventType("v1.test_event2")]
    private class TestEvent2 { }
}
