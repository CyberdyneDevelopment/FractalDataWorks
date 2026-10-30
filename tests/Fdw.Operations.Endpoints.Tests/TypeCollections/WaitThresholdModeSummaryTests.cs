using System.Linq;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that the modes of thresholds on waiting runs tell the console, through the type-collection route, which levels each may be stated at and which one carries
/// thresholds (FDW-3161), so the console holds neither rule.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class WaitThresholdModeSummaryTests
{
    [Fact]
    public void TheServerIsOfferedOnlyTheModesThatListItAndNotInherit()
    {
        var offeredToTheServer = WaitThresholdModes.All()
            .Select(mode => TypeCollectionResolver.ToSummary(mode))
            .Where(summary => summary.Levels!.Contains("Server"))
            .Select(summary => summary.Name);

        offeredToTheServer.ShouldBe(["Own", "None"], ignoreOrder: true);
    }

    [Fact]
    public void APipelineAndAScheduleAreOfferedEveryMode()
    {
        foreach (var level in new[] { "Pipeline", "Schedule" })
        {
            WaitThresholdModes.All().Select(mode => TypeCollectionResolver.ToSummary(mode))
                .Where(summary => summary.Levels!.Contains(level)).Select(summary => summary.Name)
                .ShouldBe(["Inherit", "Own", "None"], ignoreOrder: true);
        }
    }

    [Fact]
    public void OnlyOwnCarriesThresholdsAndNamesThePropertyThatHoldsThem()
    {
        var summaries = WaitThresholdModes.All().Select(mode => TypeCollectionResolver.ToSummary(mode)).ToDictionary(summary => summary.Name);

        summaries["Own"].RequiredProperties.ShouldBe(["thresholds"]);
        summaries["Own"].ExpectedProperties.ShouldBe(["thresholds"]);
        summaries["Inherit"].RequiredProperties.ShouldBeEmpty();
        summaries["None"].RequiredProperties.ShouldBeEmpty();
    }

    [Fact]
    public void AnOptionThatStatesNoLevelsListsNone()
    {
        TypeCollectionResolver.ToSummary(new InsertLoadStepKind()).Levels.ShouldBeNull();
    }

    // The generated NotFound sentinel calls the base constructor with default arguments when the collection is first touched; a constructor parameter it cannot default aborts
    // every host at start (FDW-2931), so the sentinel is built here.
    [Fact]
    public void TheCollectionBuildsItsNotFoundSentinel()
    {
        WaitThresholdModes.NotFound.ShouldNotBeNull();
    }
}
