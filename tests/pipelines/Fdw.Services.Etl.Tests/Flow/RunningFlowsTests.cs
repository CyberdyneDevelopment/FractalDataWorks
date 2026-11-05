using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the live view's registry (FDW-3382): a run is listed from the moment its pipeline begins, a component shows no figure until it starts, and a run that has
/// ended is no longer listed.
/// </summary>
public sealed class RunningFlowsTests
{
    private static readonly IReadOnlyList<FlowLiveComponent> Components =
        [new("src", 0, true), new("dst", 1, false)];

    private static readonly FlowLink SrcToDst = new("src", "output", "dst", "input");

    private static Func<IReadOnlyList<FlowLinkCount>> Carrying(long rows) => () => [new FlowLinkCount(SrcToDst, rows, 1, rows * 8)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunTheHostDoesNotFollowAnswersNothingNotAnEmptyList()
    {
        new RunningFlows().Components(Guid.NewGuid()).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AGraphThatHasNotStartedListsEveryComponentWaitingWithNoFigures()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(5));

        var listed = flows.Components(id)!;

        listed.Select(c => c.Name).ShouldBe(["src", "dst"], ignoreOrder: true);
        listed.ShouldAllBe(c => c.StartedAt == null && c.EndedAt == null && c.Succeeded == null && c.Rows == null);
        listed.Single(c => c.Name == "dst").Stage.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStartedComponentShowsTheRowsItHasPassedOnAndADestinationTheRowsThatReachedIt()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));

        graph.ComponentStarted("src");
        graph.ComponentStarted("dst");
        var listed = flows.Components(id)!;

        var src = listed.Single(c => c.Name == "src");
        src.StartedAt.ShouldNotBeNull();
        src.EndedAt.ShouldBeNull();
        src.Rows.ShouldBe(7);
        listed.Single(c => c.Name == "dst").Rows.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentThatWaitsStillShowsNoRowsWhileAnotherHasStarted()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));

        graph.ComponentStarted("src");

        flows.Components(id)!.Single(c => c.Name == "dst").Rows.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnEndedComponentKeepsItsTimesAndHowItEnded()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));

        graph.ComponentStarted("src");
        graph.ComponentEnded("src", succeeded: false);

        var src = flows.Components(id)!.Single(c => c.Name == "src");
        src.EndedAt.ShouldNotBeNull();
        src.EndedAt!.Value.ShouldBeGreaterThanOrEqualTo(src.StartedAt!.Value);
        src.Succeeded.ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentTheGraphDoesNotHaveIsRefusedNotIgnored()
    {
        var flows = new RunningFlows();
        using var run = flows.Begin(Guid.NewGuid());
        using var graph = run.Open(Components, Carrying(1));

        Should.Throw<ArgumentException>(() => graph.ComponentStarted("nope"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ADisposedGraphLeavesTheViewAndADisposedRunIsNoLongerFollowed()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        var run = flows.Begin(id);
        var graph = run.Open(Components, Carrying(1));

        graph.Dispose();
        flows.Components(id).ShouldBeEmpty();

        run.Dispose();
        flows.Components(id).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TwoGraphsOfOneRunAreListedTogether()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var first = run.Open([new("a", 0, true)], () => []);
        using var second = run.Open([new("b", 0, true)], () => []);

        flows.Components(id)!.Select(c => c.Name).ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStartedComponentShowsTheRowsThatArrivedAtItWhetherOrNotItPassesAnyOn()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));
        graph.ComponentStarted("src");
        graph.ComponentStarted("dst");

        var listed = flows.Components(id)!;

        listed.Single(c => c.Name == "src").RowsIn.ShouldBe(0);
        listed.Single(c => c.Name == "dst").RowsIn.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentThatHasNotStartedShowsNoRowsIn()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));

        flows.Components(id)!.ShouldAllBe(c => c.RowsIn == null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunListsWhatEveryLinkHasCarriedAndARunTheHostDoesNotFollowAnswersNothing()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        using var graph = run.Open(Components, Carrying(7));
        flows.Links(id)!.ShouldBeEmpty();

        graph.ComponentStarted("src");

        var link = flows.Links(id)!.ShouldHaveSingleItem();
        link.Link.FromComponent.ShouldBe("src");
        link.Link.ToComponent.ShouldBe("dst");
        link.Rows.ShouldBe(7);
        flows.Links(Guid.NewGuid()).ShouldBeNull();
    }
}
