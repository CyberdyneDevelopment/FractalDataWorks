using System.Linq;
using Fdw.Operations.Endpoints.TypeCollections;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that the type-collection route serves the comparison operators and the alert severities a threshold on waiting runs is saved with (FDW-3292): the route finds a
/// collection by name and reads its options, and an option the registration generator skipped in a consuming assembly leaves the collection empty and the route answering 404.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class ThresholdOptionCollectionsTests
{
    [Theory]
    [InlineData("ComparisonOperators", new[] { "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual" })]
    [InlineData("AlertSeverities", new[] { "Info", "Warning", "Error", "Critical" })]
    public void TheRouteFindsTheCollectionAndListsEveryOptionAThresholdCanName(string collection, string[] expected)
    {
        var type = TypeCollectionResolver.FindCollectionType(collection);

        type.ShouldNotBeNull();
        TypeCollectionResolver.GetValues(type).Select(option => option.Name).ShouldBe(expected, ignoreOrder: true);
    }
}
