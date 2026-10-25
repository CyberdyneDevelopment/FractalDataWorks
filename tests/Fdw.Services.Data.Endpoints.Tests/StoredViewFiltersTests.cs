using System.Linq;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-2391: the stored filters of a view and its parents are read as one filter tree, ANDed parent first, and a filter that cannot
/// be read is refused by naming the view, never skipped.
/// </summary>
public sealed class StoredViewFiltersTests
{
    private const string Grade = """{"field":"grade","operator":"Equal","value":"07"}""";
    private const string Campus = """{"field":"campus","operator":"Equal","value":"North"}""";

    [Fact]
    public void ADataSetHasNoFilter()
    {
        var result = StoredViewFilters.Of([], "AfcPlayers", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void OneStoredFilterIsTheFilterItself()
    {
        var result = StoredViewFilters.Of([Grade], "weekly", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Field.ShouldBe("grade");
        result.Value.Nodes.ShouldBeNull();
    }

    [Fact]
    public void SeveralStoredFiltersAreCombinedWithAndInTheOrderGiven()
    {
        var result = StoredViewFilters.Of([Grade, Campus], "weekly-north", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Logic.ShouldBe("And");
        result.Value.Nodes!.Select(node => node.Field).ShouldBe(new[] { "grade", "campus" });
    }

    [Fact]
    public void AStoredFilterThatIsNotAFilterTreeIsRefusedAndNotSkipped()
    {
        var result = StoredViewFilters.Of([Grade, "{not json"], "weekly", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("weekly");
    }

    [Fact]
    public void AStoredFilterThatIsJsonNullIsRefused()
    {
        var result = StoredViewFilters.Of(["null"], "weekly", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }
}
