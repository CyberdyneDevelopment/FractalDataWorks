using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for judging a level of thresholds on waiting runs before it is saved (FDW-3161): the mode exists and may be stated by its owner, a mode that states thresholds has some
/// and one that does not has none, and every threshold states every member, names a comparison and a severity that exist, and does something. The first thing wrong is refused,
/// naming the member; nothing is filled in.
/// </summary>
public sealed class WaitThresholdValidatorTests
{
    private static WaitThresholdPayload Threshold(string name = "Queue getting long") => new()
    {
        Name = name, Comparison = "GreaterThanOrEqual", Number = 50, Severity = "Warning", NotificationRules = ["Ops email"], StopsNewRuns = false, IsEnabled = true,
    };

    private static WaitThresholdLevelPayload Own(params WaitThresholdPayload[] thresholds) => new() { Mode = "Own", Thresholds = thresholds };

    private static Fdw.Results.IGenericResult Check(string owner, WaitThresholdLevelPayload level) => WaitThresholdValidator.Check(owner, level, NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnOwnLevelWithAWellFormedThresholdIsAccepted()
    {
        Check("Server", Own(Threshold())).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Inherit", "Pipeline")]
    [InlineData("None", "Server")]
    [InlineData("None", "Schedule")]
    public void AModeWithNoThresholdsIsAcceptedAtTheLevelsItNames(string mode, string owner)
    {
        Check(owner, new WaitThresholdLevelPayload { Mode = mode }).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheServerCannotInheritBecauseItHasNoLevelAboveIt()
    {
        var checkedLevel = Check("Server", new WaitThresholdLevelPayload { Mode = "Inherit" });

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain("mode");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sometimes")]
    public void AModeThatIsMissingOrDoesNotExistIsRefusedNamingTheMode(string? mode)
    {
        var checkedLevel = Check("Pipeline", new WaitThresholdLevelPayload { Mode = mode });

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain("mode");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnOwnLevelWithNoThresholdsAndANoneLevelWithSomeAreRefused()
    {
        Check("Pipeline", Own()).IsFailure.ShouldBeTrue();
        Check("Pipeline", new WaitThresholdLevelPayload { Mode = "None", Thresholds = [Threshold()] }).IsFailure.ShouldBeTrue();
        Check("Pipeline", new WaitThresholdLevelPayload { Mode = "Inherit", Thresholds = [Threshold()] }).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AThresholdThatDoesNothingIsRefusedNamingIt()
    {
        var doesNothing = Threshold();
        doesNothing.NotificationRules = [];

        var checkedLevel = Check("Server", Own(doesNothing));

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain("thresholds[0]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AThresholdThatOnlyStopsNewRunsDoesSomething()
    {
        var stops = Threshold();
        stops.NotificationRules = [];
        stops.StopsNewRuns = true;

        Check("Server", Own(stops)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("name")]
    [InlineData("comparison")]
    [InlineData("severity")]
    [InlineData("number")]
    [InlineData("stopsNewRuns")]
    [InlineData("isEnabled")]
    public void AThresholdThatLeavesAMemberUnstatedIsRefusedNamingTheMember(string member)
    {
        var threshold = Threshold();
        switch (member)
        {
            case "name": threshold.Name = null; break;
            case "comparison": threshold.Comparison = null; break;
            case "severity": threshold.Severity = null; break;
            case "number": threshold.Number = null; break;
            case "stopsNewRuns": threshold.StopsNewRuns = null; break;
            default: threshold.IsEnabled = null; break;
        }

        var checkedLevel = Check("Server", Own(threshold));

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain($"thresholds[0].{member}");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("MuchBigger", "Warning", 50, "comparison")]
    [InlineData("GreaterThan", "Dire", 50, "severity")]
    [InlineData("GreaterThan", "Warning", -1, "number")]
    public void AThresholdThatNamesAnOptionThatDoesNotExistOrANegativeNumberIsRefusedNamingTheMember(string comparison, string severity, int number, string member)
    {
        var threshold = Threshold();
        threshold.Comparison = comparison;
        threshold.Severity = severity;
        threshold.Number = number;

        var checkedLevel = Check("Server", Own(threshold));

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain(member);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoThresholdsWithTheSameNameAreRefusedEvenWhenTheCaseDiffers()
    {
        var checkedLevel = Check("Server", Own(Threshold("Busy"), Threshold("BUSY")));

        checkedLevel.IsFailure.ShouldBeTrue();
        checkedLevel.CurrentMessage.ShouldNotBeNull().ShouldContain("thresholds[1].name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABlankOrRepeatedNotificationRuleIsRefused()
    {
        var blank = Threshold();
        blank.NotificationRules = new List<string> { "Ops email", " " };
        var twice = Threshold();
        twice.NotificationRules = new List<string> { "Ops email", "ops EMAIL" };

        Check("Server", Own(blank)).IsFailure.ShouldBeTrue();
        Check("Server", Own(twice)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABlankCountedPipelineIsRefusedButNoneCountsTheWholeServer()
    {
        var blank = Threshold();
        blank.CountedPipeline = " ";
        var server = Threshold();
        server.CountedPipeline = null;

        Check("Server", Own(blank)).CurrentMessage.ShouldNotBeNull().ShouldContain("countedPipeline");
        Check("Server", Own(server)).IsSuccess.ShouldBeTrue();
    }
}
