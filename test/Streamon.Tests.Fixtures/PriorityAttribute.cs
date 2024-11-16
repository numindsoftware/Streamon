#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Xunit;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PriorityAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
