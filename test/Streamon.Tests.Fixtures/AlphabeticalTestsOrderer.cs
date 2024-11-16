using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Streamon.Tests.Fixtures;

public class AlphabeticalTestsOrderer : ITestCollectionOrderer, ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
        testCases.OrderBy(tc => tc.TestMethod.Method.Name);

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections.OrderBy(tc => tc.DisplayName, StringComparer.OrdinalIgnoreCase);
}
