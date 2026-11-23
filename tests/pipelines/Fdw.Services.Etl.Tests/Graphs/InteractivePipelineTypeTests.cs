using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6141 slice 1: the Interactive pipeline type is a second closing of the graph pipeline type (one graph row container, every graph component unchanged) whose runs
/// are carried out in the AtOnce run mode, and what a type requires of its configuration is stated by the type, refused when the configuration cannot state it
/// (at Initialize, every gap in one pass) and when a row leaves it out (at the save).
/// </summary>
[Trait("Category", "InteractivePipelineType")]
public sealed class InteractivePipelineTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void InteractiveIsAPipelineTypeCarriedOutAtOnce()
    {
        var type = PipelineTypes.ByName("Interactive");

        ReferenceEquals(type, PipelineTypes.NotFound).ShouldBeFalse();
        type.RunModeName.ShouldBe("AtOnce");
        PipelineRunModes.ByName(type.RunModeName).ShouldNotBeSameAs(PipelineRunModes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void InteractiveRequiresTheHalfDoneRuleTheTimeLimitTheStepLimitAndTheClaimKeyTemplate()
        => PipelineTypes.ByName("Interactive").ConfigurationRequirements.Select(requirement => requirement.FactName)
            .ShouldBe(["HalfDoneRule", "MaxRunSeconds", "MaxSteps", "ClaimKeyTemplate"], ignoreOrder: true);

    [Fact]
    [Trait("Priority", "P0")]
    public void GraphRequiresNothingAndStillAllowsEachFact()
    {
        PipelineTypes.ByName("Graph").ConfigurationRequirements.ShouldBeEmpty();

        var allowed = new GraphPipelineConfiguration { Implementation = "Graph", MaxRunSeconds = 30, MaxSteps = 10, HalfDoneRule = "End", ClaimKeyTemplate = "{conversation}" };
        allowed.ShouldBeAssignableTo<IRunTimeLimitPipelineConfiguration>().MaxRunSeconds.ShouldBe(30);
        allowed.ShouldBeAssignableTo<IHalfDoneRulePipelineConfiguration>().HalfDoneRule.ShouldBe("End");
        allowed.ShouldBeAssignableTo<IClaimKeyPipelineConfiguration>().ClaimKeyTemplate.ShouldBe("{conversation}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATypeWhoseConfigurationCannotStateWhatItRequiresIsAGapAtInitializeNamingTheTypeAndTheFact()
    {
        var type = new Mock<IPipelineType>();
        type.SetupGet(t => t.Name).Returns("Lonely");
        type.SetupGet(t => t.ConfigurationType).Returns(typeof(PlainConfiguration));
        type.SetupGet(t => t.ConfigurationRequirements).Returns(
        [
            PipelineConfigurationRequirement.Of<IHalfDoneRulePipelineConfiguration>("HalfDoneRule", configuration => configuration.HalfDoneRule is not null),
            PipelineConfigurationRequirement.Of<IRunTimeLimitPipelineConfiguration>("MaxRunSeconds", configuration => configuration.MaxRunSeconds is not null),
        ]);

        var gaps = PipelineConfigurationRequirements.Gaps(type.Object, NullLogger.Instance);

        gaps.Count.ShouldBe(2);
        gaps.Select(gap => gap.Code).Distinct().ShouldHaveSingleItem().ShouldBe("PIPELINES-60839");
        gaps.Select(gap => gap.Message).ShouldAllBe(text => text.Contains("Lonely", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARowThatLeavesOutWhatItsTypeRequiresIsRefusedOnceForEachFact()
    {
        var type = PipelineTypes.ByName("Interactive");

        var missing = PipelineConfigurationRequirements.Missing(type, new GraphPipelineConfiguration { Implementation = "Interactive", MaxRunSeconds = 30 }, NullLogger.Instance);

        missing.Count.ShouldBe(3);
        missing.Select(message => message.Code).Distinct().ShouldHaveSingleItem().ShouldBe("PIPELINES-20840");
        missing.Select(message => message.Message).ShouldContain(text => text.Contains("HalfDoneRule", StringComparison.Ordinal));
        missing.Select(message => message.Message).ShouldContain(text => text.Contains("MaxSteps", StringComparison.Ordinal));
        missing.Select(message => message.Message).ShouldContain(text => text.Contains("ClaimKeyTemplate", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARowThatStatesEverythingItsTypeRequiresIsNotRefused()
        => PipelineConfigurationRequirements.Missing(
            PipelineTypes.ByName("Interactive"),
            new GraphPipelineConfiguration { Implementation = "Interactive", MaxRunSeconds = 30, MaxSteps = 20, HalfDoneRule = "End", ClaimKeyTemplate = "{conversation}" },
            NullLogger.Instance).ShouldBeEmpty();

    private sealed class PlainConfiguration;
}
