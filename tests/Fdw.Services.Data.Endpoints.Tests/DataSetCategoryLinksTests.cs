using System;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// Tests for how a data set and the category tree meet (FDW-1473): a request names a category by its stable name and an answer
/// carries the name and the path, a name that is not a category is refused, and a data set that cites a category that is gone is a fault.
/// </summary>
public sealed class DataSetCategoryLinksTests
{
    private static readonly Guid Sports = Guid.CreateVersion7();
    private static readonly Guid Nfl = Guid.CreateVersion7();

    private static CategoryTree Tree()
    {
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(Sports, null, "sports", "Sports", null, "DataSet"),
                new CategoryNode(Nfl, Sports, "nfl", "NFL", null, "DataSet"),
            ],
            out var problems);
        problems.ShouldBeEmpty();
        return tree.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameThatIsACategoryResolvesToItsIdentity()
    {
        var resolved = DataSetCategoryLinks.Resolve(Tree(), "nfl", NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBe(Nfl);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NoNameMeansNoCategoryAndAnUnknownNameIsRefused()
    {
        DataSetCategoryLinks.Resolve(Tree(), null, NullLogger.Instance).Value.ShouldBeNull();
        DataSetCategoryLinks.Resolve(Tree(), "  ", NullLogger.Instance).Value.ShouldBeNull();

        var refused = DataSetCategoryLinks.Resolve(Tree(), "weather", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("weather");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADataSetIsLabelledWithItsCategorysNameAndPath()
    {
        var label = DataSetCategoryLinks.Label(Tree(), Nfl, NullLogger.Instance);

        label.IsSuccess.ShouldBeTrue(label.CurrentMessage);
        label.Value.ShouldBe(new CategoryLabel("nfl", "Sports / NFL"));
        DataSetCategoryLinks.Label(Tree(), null, NullLogger.Instance).Value.ShouldBe(new CategoryLabel(null, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADataSetThatCitesACategoryThatIsNotInTheTreeIsAFaultNotAnEmptyLabel()
    {
        var label = DataSetCategoryLinks.Label(Tree(), Guid.CreateVersion7(), NullLogger.Instance);

        label.IsFailure.ShouldBeTrue();
    }
}
