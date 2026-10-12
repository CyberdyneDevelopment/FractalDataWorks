using System.Linq;
using Fdw.Web.RestEndpoints.Query;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>FDW-1032: a route's scope filter narrows the caller's own, with AND, and never replaces it.</summary>
public sealed class DataQueryScopeTests
{
    private static QueryFilterNode Condition(string field) => new() { Field = field, Operator = "Equal" };

    [Fact]
    public void NeitherSideHavingAFilterIsNoFilter()
        => DataQueryScope.Combine(null, null).ShouldBeNull();

    [Fact]
    public void OnlyTheScopeIsTheScope()
    {
        var scope = Condition("grade");

        DataQueryScope.Combine(scope, null).ShouldBeSameAs(scope);
    }

    [Fact]
    public void OnlyTheCallersFilterIsTheCallersFilter()
    {
        var requested = Condition("campus");

        DataQueryScope.Combine(null, requested).ShouldBeSameAs(requested);
    }

    [Fact]
    public void BothAreCombinedWithAndScopeFirst()
    {
        var combined = DataQueryScope.Combine(Condition("grade"), Condition("campus"));

        combined!.Logic.ShouldBe("And");
        combined.Nodes!.Select(n => n!.Field).ToArray().ShouldBe(new[] { "grade", "campus" });
    }
}
