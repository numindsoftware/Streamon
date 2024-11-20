using System.Text.Json.Serialization;

namespace Streamon;

[JsonConverter(typeof(IIdentityJsonConverterFactory))]
public interface IIdentity<T, V> where T : IIdentity<T, V>
{
    V Value { get; }

    static abstract T New();
    static abstract T From(V value);
    virtual static explicit operator T(V value) => T.From(value);
    virtual static explicit operator V(T id) => id.Value;
    abstract string ToString();
}
