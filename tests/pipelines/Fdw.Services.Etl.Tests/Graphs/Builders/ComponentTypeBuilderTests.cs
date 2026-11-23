using System;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Graphs.Contributions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs.Builders;

/// <summary>
/// FDW-6240: a ComponentTypes option says what a component is and its builder says how its step is made, two classes joined by the name. When the host
/// starts, every component type has exactly one builder of the right collection (a step builder for an action, a component builder otherwise) and every
/// builder names a component type; each mismatch is its own coded message.
/// </summary>
public sealed class ComponentTypeBuilderTests
{
    private const string NoBuilder = "FLOW-60357";
    private const string TwoBuilders = "FLOW-60364";
    private const string NamesNoType = "FLOW-60365";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AComponentTypeWithNoBuilderIsNamed()
    {
        var found = GraphFlowBuilder.TypeBuilderMismatches([new LoneComponentType("Orphan")], [], [], NullLogger.Instance).ShouldHaveSingleItem();

        found.Code.ShouldBe(NoBuilder);
        found.Message.ShouldContain("Orphan");
        found.Message.ShouldContain("step builder");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AnActionTypeWhoseOnlyBuilderIsAComponentBuilderHasNoBuilder()
    {
        var found = GraphFlowBuilder.TypeBuilderMismatches([new LoneComponentType("Ask")], [], [new NamedComponentBuilder("Ask")], NullLogger.Instance).ShouldHaveSingleItem();

        found.Code.ShouldBe(NoBuilder);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AComponentTypeWithTwoBuildersIsNamed()
    {
        var found = GraphFlowBuilder.TypeBuilderMismatches(
            [new LoneComponentType("Ask")], [new NeedingStepBuilder("Ask")], [new NamedComponentBuilder("Ask")], NullLogger.Instance).ShouldHaveSingleItem();

        found.Code.ShouldBe(TwoBuilders);
        found.Message.ShouldContain("Ask");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ABuilderThatNamesNoComponentTypeIsNamed()
    {
        var found = GraphFlowBuilder.TypeBuilderMismatches([], [new NeedingStepBuilder("Stray")], [], NullLogger.Instance).ShouldHaveSingleItem();

        found.Code.ShouldBe(NamesNoType);
        found.Message.ShouldContain("Stray");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EachMismatchIsItsOwnMessage()
    {
        var found = GraphFlowBuilder.TypeBuilderMismatches([new LoneComponentType("Orphan")], [new NeedingStepBuilder("Stray")], [], NullLogger.Instance);

        found.Select(message => message.Code).Order(StringComparer.Ordinal).ShouldBe([NoBuilder, NamesNoType]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FdwsOwnComponentTypesAndBuildersMatch()
    {
        GraphFlowBuilder.TypeBuilderMismatches(ComponentTypes.All(), FlowStepBuilders.All(), FlowComponentBuilders.All(), NullLogger.Instance).ShouldBeEmpty();
    }
}
