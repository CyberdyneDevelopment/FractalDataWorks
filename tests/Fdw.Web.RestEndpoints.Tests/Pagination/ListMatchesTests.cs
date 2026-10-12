using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-1194: a request names a match by its word, matched exactly; each match states the filter operator it means.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class ListMatchesTests
{
    [Theory]
    [InlineData("contains", typeof(ContainsListMatch), "Contains")]
    [InlineData("startsWith", typeof(StartsWithListMatch), "StartsWith")]
    [InlineData("exact", typeof(ExactListMatch), "Equal")]
    public void AWordNamesItsMatchAndTheOperatorItMeans(string wire, System.Type match, string operatorName)
    {
        var found = ListMatches.ByWire(wire);

        found.ShouldBeOfType(match);
        found.Operator.Name.ShouldBe(operatorName);
    }

    [Theory]
    [InlineData("Contains")]
    [InlineData("like")]
    [InlineData("")]
    public void AWordThatNamesNoMatchIsNotFound(string wire)
    {
        ReferenceEquals(ListMatches.ByWire(wire), ListMatches.NotFound).ShouldBeTrue();
    }
}
