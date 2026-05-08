namespace Streamon.Tests.Fixtures;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PriorityAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
