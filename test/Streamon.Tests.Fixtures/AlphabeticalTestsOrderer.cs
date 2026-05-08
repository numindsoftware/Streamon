using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Streamon.Tests.Fixtures;

public class AlphabeticalTestsOrderer : ITestCollectionOrderer, ITestCaseOrderer
{
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase =>
        testCases.OrderBy(tc => tc.TestMethodName).CastOrToReadOnlyCollection();

    public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(IReadOnlyCollection<TTestCollection> testCollections) where TTestCollection : ITestCollection =>
        testCollections.OrderBy(tc => tc.TestCollectionDisplayName, StringComparer.OrdinalIgnoreCase).CastOrToReadOnlyCollection();
}
