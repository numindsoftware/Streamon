using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Streamon.Tests.Fixtures;

public class PriorityTestCollectionOrderer : ITestCaseOrderer//, ITestCollectionOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        return testCases
                .Select(tc => new
                {
                    Order = tc.TestMethod.Method.GetCustomAttributes(typeof(PriorityAttribute).AssemblyQualifiedName).FirstOrDefault()?.GetNamedArgument<int>(nameof(PriorityAttribute.Order)) ?? 0,
                    TestCase = tc
                })
                .OrderBy(tc => tc.Order)
                .Select(tc => tc.TestCase);
    }

    //public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
    //    testCollections
    //        .Select(tc => new
    //        {
    //            Order = tc.CollectionDefinition.GetCustomAttributes(typeof(PriorityAttribute).AssemblyQualifiedName).FirstOrDefault()?.GetNamedArgument<int>(nameof(PriorityAttribute.Order)) ?? 0,
    //            TestCollection = tc
    //        })
    //        .OrderBy(tc => tc.Order)
    //        .Select(tc => tc.TestCollection);
}
