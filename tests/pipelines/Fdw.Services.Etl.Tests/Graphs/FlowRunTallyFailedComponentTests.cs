using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-4192: the tally of a run remembers the component whose failure stopped the first graph that failed, so the run's failure can be recorded against that
/// component's connection.
/// </summary>
public sealed class FlowRunTallyFailedComponentTests
{
    private static FlowRunTally Tally(params FlowRunOutcome[] outcomes)
    {
        var queue = new Queue<FlowRunOutcome>(outcomes);
        var inner = new Mock<IFlowExecutor>();
        inner
            .Setup(e => e.Execute(It.IsAny<FlowGraph>(), It.IsAny<FlowRunOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => queue.Dequeue());
        return new FlowRunTally(inner.Object);
    }

    private static FlowRunOutcome Failed(string component) =>
        new(GenericResult.Failure(new GenericMessage("it failed")), new FlowRunReport([], TimeSpan.Zero, [], component));

    private static Task<FlowRunOutcome> Run(FlowRunTally tally) =>
        tally.Execute(new FlowGraph([], [], []), new FlowRunOptions { LinkCapacity = 2 }, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AGraphThatFailsLeavesTheNameOfTheComponentThatStoppedIt()
    {
        var tally = Tally(Failed("src"));

        await Run(tally);

        tally.FailedComponent.ShouldBe("src");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASecondFailureDoesNotReplaceTheFirstThatEndedTheRun()
    {
        var tally = Tally(Failed("src"), Failed("dst"));

        await Run(tally);
        await Run(tally);

        tally.FailedComponent.ShouldBe("src");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatHasNotFailedNamesNoComponent()
    {
        var tally = Tally(new FlowRunOutcome(GenericResult.Success(), new FlowRunReport([], TimeSpan.Zero, [])));

        await Run(tally);

        tally.FailedComponent.ShouldBeNull();
    }
}
