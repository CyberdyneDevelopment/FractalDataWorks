using System;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6134: how deep a graph nests the components that hold components (a container, a loop), and the save's judgment of it against the setting MaxContainerDepth: a graph that nests
/// none needs no setting, a graph that does is refused when the setting is not set, and when it nests deeper than the setting says. No depth is assumed.
/// </summary>
[Trait("Category", "ContainerDepth")]
public sealed class GraphContainerDepthTests
{
    private static GraphPipelineConfiguration Graph(params (string Name, string Kind, string? Parent)[] components)
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample" };
        foreach (var (name, kind, parent) in components)
        {
            graph.Components.Add(new PipelineComponentConfiguration { Name = name, ComponentType = kind, ParentComponent = parent });
        }

        return graph;
    }

    private static Mock<IEffectiveSettingsProvider> Setting(int maximum)
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxContainerDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(maximum);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphThatNestsNothingHasNoDepth() =>
        GraphContainerDepth.Deepest(Graph(("a", "SetVariable", null), ("b", "Filter", null))).ShouldBe((null, 0));

    [Fact]
    [Trait("Priority", "P0")]
    public void ALoopAtTheTopIsOneLevelAndAHolderInsideEachHolderAddsOne()
    {
        GraphContainerDepth.Deepest(Graph(("loop", "ForEach", null), ("body", "SetVariable", "loop"))).ShouldBe(("loop", 1));
        GraphContainerDepth.Deepest(Graph(("outer", "Container", null), ("middle", "DoWhile", "outer"), ("inner", "Container", "middle"), ("leaf", "SetVariable", "inner")))
            .ShouldBe(("inner", 3));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void HoldersSideBySideDoNotAddUp() =>
        GraphContainerDepth.Deepest(Graph(("one", "Container", null), ("two", "Container", null), ("child", "ForLoop", "one"))).ShouldBe(("child", 2));

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphThatNestsNothingIsNotJudgedAndNeedsNoSetting()
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);

        GraphContainerDepthJudgment.Judge(Graph(("a", "SetVariable", null)), settings.Object, null, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphThatNestsWhenTheSettingIsNotSetIsRefusedNamingTheSetting()
    {
        var judged = GraphContainerDepthJudgment.Judge(Graph(("box", "Container", null)), Setting(0).Object, null, NullLogger.Instance);

        judged.IsFailure.ShouldBeTrue();
        judged.CurrentMessage.ShouldNotBeNull().ShouldContain(SettingDefinitions.MaxContainerDepth);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphNestedDeeperThanTheSettingIsRefusedNamingTheComponentTheDepthAndTheMost()
    {
        var graph = Graph(("outer", "Container", null), ("inner", "Container", "outer"), ("deepest", "Container", "inner"));

        var judged = GraphContainerDepthJudgment.Judge(graph, Setting(2).Object, null, NullLogger.Instance);

        judged.IsFailure.ShouldBeTrue();
        judged.CurrentMessage.ShouldNotBeNull().ShouldContain("deepest");
        judged.CurrentMessage.ShouldContain("nests 3 levels");
        judged.CurrentMessage.ShouldContain("at most 2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphNestedExactlyAsDeepAsTheSettingSaysIsAccepted() =>
        GraphContainerDepthJudgment.Judge(Graph(("outer", "Container", null), ("inner", "Container", "outer")), Setting(2).Object, null, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
}
