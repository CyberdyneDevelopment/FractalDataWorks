using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>Tests for which categories count toward a failure threshold (FDW-4005): only the Refused category, the TooManyRequests category (FDW-2335) and the non-failure category do not.</summary>
public sealed class ResultCategoryCountsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void OnlyRefusedTooManyRequestsAndSuccessDoNotCountTowardAFailureThreshold()
    {
        ResultCategories.All().Where(category => !category.CountsTowardFailureThresholds).Select(category => category.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()
            .ShouldBe(["Refused", "Success", "TooManyRequests"]);
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

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void PayloadTooLargeIsCategoryThirteenAnswering413AndCounts()
    {
        var tooLarge = ResultCategories.ByName("PayloadTooLarge");

        tooLarge.Id.ShouldBe(13);
        tooLarge.IsFailure.ShouldBeTrue();
        tooLarge.IsRetryable.ShouldBeFalse();
        tooLarge.HttpStatus.ShouldBe(413);
        tooLarge.CountsTowardFailureThresholds.ShouldBeTrue();
        ResultCategories.ById(130000 / 10000).ShouldBe(tooLarge);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("NotAcceptable", 14, 406, false, true)]
    [InlineData("TooManyRequests", 15, 429, true, false)]
    public void TheStatusCategoriesDeclareTheirIdStatusRetryAndThresholdBehaviour(string name, int id, int status, bool retryable, bool counts)
    {
        var category = ResultCategories.ByName(name);

        category.Id.ShouldBe(id);
        category.IsFailure.ShouldBeTrue();
        category.HttpStatus.ShouldBe(status);
        category.IsRetryable.ShouldBe(retryable);
        category.CountsTowardFailureThresholds.ShouldBe(counts);
        ResultCategories.ById(id * 10000 / 10000).ShouldBe(category);
    }
}
