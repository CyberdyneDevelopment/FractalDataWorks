using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for what the executor tells the live view (FDW-3382): each component is listed waiting until its stage releases it, shows as started while it runs,
/// and as ended when it is done, so a reader of the view sees the stage a run is in.
/// </summary>
public sealed class FlowLiveTests
{
    private readonly FlowExecutor _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheViewSeesTheEarlierStageRunningAndTheLaterStageWaitingThenTheReverse()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        IReadOnlyList<FlowLiveComponentView>? whileFirst = null;
        IReadOnlyList<FlowLiveComponentView>? whileSecond = null;
        var first = new ScriptedComponent("first", [], ["output"], (_, _) =>
        {
            whileFirst = flows.Components(id);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });
        var second = new ScriptedComponent("second", [], ["output"], (_, _) =>
        {
            whileSecond = flows.Components(id);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });
        // Why a dropped output on each: the validator refuses a component with no inputs and no outputs, and a source output must be linked or dropped.
        var graph = new FlowGraph([first, second], [], [new FlowDroppedOutput("first", "output"), new FlowDroppedOutput("second", "output")], [new FlowStage("first", 1), new FlowStage("second", 2)]);
        var options = new FlowRunOptions { LinkCapacity = 2, Live = run };

        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await _sut.Run(graph, options, guard.Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        whileFirst.ShouldNotBeNull();
        var firstThen = whileFirst!.Single(c => c.Name == "first");
        firstThen.Stage.ShouldBe(1);
        firstThen.StartedAt.ShouldNotBeNull();
        firstThen.EndedAt.ShouldBeNull();
        var secondThen = whileFirst!.Single(c => c.Name == "second");
        secondThen.Stage.ShouldBe(2);
        secondThen.StartedAt.ShouldBeNull();
        whileSecond.ShouldNotBeNull();
        var firstLater = whileSecond!.Single(c => c.Name == "first");
        firstLater.EndedAt.ShouldNotBeNull();
        firstLater.Succeeded.ShouldBe(true);
        whileSecond!.Single(c => c.Name == "second").EndedAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARowsCountComesFromTheLineWhileTheComponentRuns()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        IReadOnlyList<FlowLiveComponentView>? seen = null;
        var (sink, _) = Sink("dst");
        var watch = new ScriptedComponent("watch", ["input"], ["output"], async (ports, ct) =>
        {
            var input = ports.Input("input");
            var output = ports.Output("output");
            var first = true;
            while (await input.Read(ct) is { } batch)
            {
                if (!first)
                {
                    seen ??= flows.Components(id);
                }

                first = false;
                await output.Write(batch, ct);
            }

            return GenericResult.Success();
        });
        var graph = Graph([Source("src", 30, batchSize: 10), watch, sink], [Link("src", "watch"), Link("watch", "dst")]);
        var options = new FlowRunOptions { LinkCapacity = 1, Live = run };

        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await _sut.Run(graph, options, guard.Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        seen.ShouldNotBeNull();
        var rows = seen!.Single(c => c.Name == "src").Rows;
        rows.ShouldNotBeNull();
        rows!.Value.ShouldBeGreaterThanOrEqualTo(10);
        flows.Components(id)!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailedComponentIsListedAsEndedWithoutSuccess()
    {
        var flows = new RunningFlows();
        var id = Guid.NewGuid();
        using var run = flows.Begin(id);
        var boom = new ScriptedComponent("boom", [], ["output"], (_, _) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("boom"))));
        var graph = new FlowGraph([boom], [], [new FlowDroppedOutput("boom", "output")]);
        var snapshotting = new Snapshotting(run, flows, id);
        var options = new FlowRunOptions { LinkCapacity = 2, Live = snapshotting };

        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await _sut.Run(graph, options, guard.Token);

        result.IsFailure.ShouldBeTrue();
        snapshotting.Last.ShouldNotBeNull();
        var ended = snapshotting.Last!.Single(c => c.Name == "boom");
        ended.Succeeded.ShouldBe(false);
        ended.EndedAt.ShouldNotBeNull();
    }

    // Reads the view just before the graph is disposed, which is when a failed component has ended and the graph is still listed.
    private sealed class Snapshotting(RunLive inner, RunningFlows flows, Guid id) : IFlowLiveRun
    {
        public IReadOnlyList<FlowLiveComponentView>? Last { get; private set; }

        public IFlowLiveGraph Open(IReadOnlyList<FlowLiveComponent> components, Func<IReadOnlyList<FlowLinkCount>> links)
            => new Reading(inner.Open(components, links), () => Last = flows.Components(id));
    }

    private sealed class Reading(IFlowLiveGraph inner, Action beforeDispose) : IFlowLiveGraph
    {
        public void ComponentStarted(string component) => inner.ComponentStarted(component);

        public void ComponentEnded(string component, bool succeeded) => inner.ComponentEnded(component, succeeded);

        public void Dispose()
        {
            beforeDispose();
            inner.Dispose();
        }
    }
}
