using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for <see cref="FlowGraphValidator"/> (FDW-1386): a graph that cannot run is refused with the name that breaks the rule,
/// before anything starts.
/// </summary>
public sealed class FlowGraphValidatorTests
{
    private static FlowSource Src(string name = "src") => Source(name, 1);

    private static FlowDestination Dst(string name = "dst") => Sink(name).Component;

    private static void Refused(FlowGraph graph, string reason)
    {
        var result = FlowGraphValidator.Validate(graph, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ASourceLinkedToADestinationIsValid()
    {
        var result = FlowGraphValidator.Validate(Graph([Src(), Dst()], [Link("src", "dst")]), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphWithNoComponentsIsRefused() => Refused(Graph([], []), "no components");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoComponentsWithTheSameNameAreRefused() =>
        Refused(Graph([Src("a"), Src("A")], []), "used twice");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASourceHasNoInputsSoALinkIntoItIsRefused() =>
        Refused(Graph([Src("a"), Src("b")], [Link("a", "b")]), "does not have");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADestinationHasNoOutputsSoALinkOutOfItIsRefused() =>
        Refused(Graph([Src(), Dst("a"), Dst("b")], [Link("src", "a"), Link("a", "b")]), "does not have");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentWithNoNodesAtAllIsRefused()
    {
        var empty = new ScriptedComponent("empty", [], [], (_, _) => throw new System.NotSupportedException());

        Refused(Graph([empty], []), "no inputs and no outputs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALinkToAComponentThatDoesNotExistIsRefused() =>
        Refused(Graph([Src()], [Link("src", "ghost")]), "does not exist");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALinkToAnInputTheComponentDoesNotHaveIsRefused() =>
        Refused(Graph([Src(), Dst()], [Link("src", "dst", input: "side")]), "does not have");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInputNothingFeedsIsRefused() =>
        Refused(Graph([Src(), Dst()], []), "is not linked to any output");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnOutputThatGoesNowhereIsRefusedUnlessItIsDropped()
    {
        var split = ScriptedComponent.Idle("mid");
        var links = new[] { Link("src", "mid") };

        Refused(Graph([Src(), split], links), "neither linked nor dropped");

        var dropped = FlowGraphValidator.Validate(
            Graph([Src(), split], links, new FlowDroppedOutput("mid", "output")), NullLogger.Instance);
        dropped.IsSuccess.ShouldBeTrue(dropped.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnOutputBothLinkedAndDroppedIsRefused() =>
        Refused(
            Graph([Src(), Dst()], [Link("src", "dst")], new FlowDroppedOutput("src", "output")),
            "both linked and dropped");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoopOfLinksIsRefusedNamingTheComponentsInIt()
    {
        var a = ScriptedComponent.Idle("a");
        var b = ScriptedComponent.Idle("b");
        var links = new List<FlowLink> { Link("src", "a"), Link("a", "b"), Link("b", "a") };

        Refused(Graph([Src(), a, b], links), "cycle");
    }
}
