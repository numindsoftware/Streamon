using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Streamon.Tests.Fixtures;

public class PriorityOrderer : ITestCaseOrderer
{
    private static ConcurrentDictionary<string, int> _defaultPriorities = new ConcurrentDictionary<string, int>();

    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : ITestCase
    {
        var groupedTestCases = new Dictionary<int, List<ITestCase>>();
        var defaultPriorities = new Dictionary<Type, int>();

        foreach (IXunitTestCase testCase in testCases)
        {
            var defaultPriority = DefaultPriorityForClass(testCase);
            var priority = PriorityForTest(testCase, defaultPriority);

            if (!groupedTestCases.ContainsKey(priority))
                groupedTestCases[priority] = new List<ITestCase>();

            groupedTestCases[priority].Add(testCase);
        }

        var result = new List<TTestCase>();

        var orderedKeys = groupedTestCases.Keys.OrderBy(k => k);
        foreach (var list in orderedKeys.Select(priority => groupedTestCases[priority]))
        {
            list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod!.MethodName, y.TestMethod!.MethodName));
            result.AddRange(list.Cast<TTestCase>());
        }

        return result;
    }

    private static int PriorityForTest(IXunitTestCase testCase, int defaultPriority)
    {
        var priorityAttribute = testCase.TestMethod.Method
            .GetCustomAttributes<PriorityAttribute>()
            .SingleOrDefault();
        return priorityAttribute?.Order ?? defaultPriority;
    }

    private static int DefaultPriorityForClass(IXunitTestCase testCase)
    {
        var testClass = testCase.TestMethod.TestClass.Class;
        if (!_defaultPriorities.TryGetValue(testClass.Name, out var result))
        {
            var defaultAttribute = testClass.GetCustomAttributes<PriorityAttribute>().SingleOrDefault();
            result = defaultAttribute?.Order ?? int.MaxValue;
            _defaultPriorities[testClass.Name] = result;
        }

        return result;
    }
}
