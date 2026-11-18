using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Options.Tests;

/// <summary>FDW-6340: a stored rule path resolves through the nested layers to a kind, one numbered failure per reason.</summary>
public sealed class RulePathTests
{
    private static readonly string[] InPath = ["DataConfigurationRules", "Comparison", "In"];

    [Fact]
    public void ThePathOfTheInKindResolvesToTheKind()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, InPath, "To");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<InComparison>();
        result.Value!.Method.ShouldBe("To");
    }

    [Fact]
    public void ThePathNamesAreMatchedWithoutRegardToCase()
    {
        RulePaths.Resolve(NullLogger.Instance, ["dataconfigurationrules", "comparison", "in"], "To").IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void TheComparisonLayerOpensTheNineKinds()
    {
        DataConfigurationRules.ByName("Comparison").ShouldBeOfType<ComparisonRuleLayer>();
        new ComparisonRuleLayer().Layers.Select(layer => layer.Name).ShouldBe(
            ["In", "NotIn", "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Between"],
            ignoreOrder: true);
    }

    [Theory]
    [InlineData("In", 1, null)]
    [InlineData("NotIn", 1, null)]
    [InlineData("Equal", 1, 1)]
    [InlineData("NotEqual", 1, 1)]
    [InlineData("GreaterThan", 1, 1)]
    [InlineData("GreaterThanOrEqual", 1, 1)]
    [InlineData("LessThan", 1, 1)]
    [InlineData("LessThanOrEqual", 1, 1)]
    [InlineData("Between", 2, 2)]
    public void EachKindStatesHowManyValuesItTakes(string kind, int minimum, int? maximum)
    {
        var resolved = RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "Comparison", kind], "To").Value!;

        resolved.MinValues.ShouldBe(minimum);
        resolved.MaxValues.ShouldBe(maximum);
    }

    [Fact]
    public void AnEmptyPathFailsAtTheFirstLevel()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, [], "To");

        result.Code!.Name.ShouldBe("DataOptionPathLevelNotFound");
        result.Code.Id.ShouldBe(30001);
    }

    [Fact]
    public void APathThatDoesNotStartAtTheRootFailsAtTheFirstLevel()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, ["DataOptions", "SelectList", "Include"], "To");

        result.Code!.Name.ShouldBe("DataOptionPathLevelNotFound");
    }

    [Fact]
    public void ALevelThatNamesNothingFailsAndNamesTheLevel()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "NoSuchLayer", "In"], "To");

        result.Code!.Name.ShouldBe("DataOptionPathLevelNotFound");
        result.Code.Id.ShouldBe(30001);
    }

    [Fact]
    public void AKindThatNamesNothingFails()
    {
        RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "Comparison", "Like"], "To").Code!.Id.ShouldBe(30001);
    }

    [Fact]
    public void APathThatStopsAtALayerWithLayersBelowItFails()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "Comparison"], "To");

        result.Code!.Name.ShouldBe("DataOptionPathEndsBeforeKind");
        result.Code.Id.ShouldBe(20002);
    }

    [Fact]
    public void APathThatStopsAtTheRootFails()
    {
        RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules"], "To").Code!.Id.ShouldBe(20002);
    }

    [Fact]
    public void APathThatGoesOnBelowAKindFails()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "Comparison", "In", "Extra"], "To");

        result.Code!.Name.ShouldBe("DataOptionPathContinuesPastKind");
        result.Code.Id.ShouldBe(20003);
    }

    [Fact]
    public void AMethodTheKindDoesNotHaveFails()
    {
        var result = RulePaths.Resolve(NullLogger.Instance, InPath, "Add");

        result.Code!.Name.ShouldBe("DataOptionMethodNotFound");
        result.Code.Id.ShouldBe(30004);
    }
}
