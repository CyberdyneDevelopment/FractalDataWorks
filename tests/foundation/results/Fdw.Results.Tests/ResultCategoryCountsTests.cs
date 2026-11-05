using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>Tests for which categories count toward a failure threshold (FDW-4005): only the Refused category and the non-failure category do not.</summary>
public sealed class ResultCategoryCountsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void OnlyRefusedAndSuccessDoNotCountTowardAFailureThreshold()
    {
        ResultCategories.All().Where(category => !category.CountsTowardFailureThresholds).Select(category => category.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()
            .ShouldBe(["Refused", "Success"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RefusedIsCategoryTwelveAFailureNotRetryableAndConflictStillCounts()
    {
        var refused = ResultCategories.ByName("Refused");

        refused.Id.ShouldBe(12);
        refused.IsFailure.ShouldBeTrue();
        refused.IsRetryable.ShouldBeFalse();
        ResultCategories.ById(120000 / 10000).ShouldBe(refused);
        ResultCategories.ByName("Conflict").CountsTowardFailureThresholds.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheNotFoundSentinelStillConstructsAndCountsAsAFailure()
    {
        ResultCategories.NotFound.CountsTowardFailureThresholds.ShouldBeTrue();
    }
}
