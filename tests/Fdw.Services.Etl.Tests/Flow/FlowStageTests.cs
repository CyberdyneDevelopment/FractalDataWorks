using System.Collections.Concurrent;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the stage of a component (FDW-2040): a component with no stage runs first, a component in a stage starts only when every component of a lower
/// stage has finished, a line may cross stages without stopping the earlier one, and a line may not go back to an earlier stage.
/// </summary>
public sealed class FlowStageTests
{
    private readonly FlowExecutor _sut = new();

    // A source of numbered rows that notes when it starts reading and when it has read the last row.
    private static FlowSource Noted(string name, int count, ConcurrentQueue<string> log) =>
        new(name, ct => NotedRows(name, count, log, ct), 3);

    private static async IAsyncEnumerable<IDictionary<string, object?>> NotedRows(
        string name, int count, ConcurrentQueue<string> log, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        log.Enqueue($"{name}-start");
        for (var i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Row(i);
            await Task.Yield();
        }

        log.Enqueue($"{name}-end");
    }

    // A component that notes when it starts and passes every batch on.
    private static ScriptedComponent NotedPass(string name, ConcurrentQueue<string> log) =>
        new(name, ["input"], ["output"], async (ports, ct) =>
        {
            log.Enqueue($"{name}-start");
            var input = ports.Input("input");
            var output = ports.Output("output");
            while (await input.Read(ct) is { } batch)
            {
                await output.Write(batch, ct);
            }

            log.Enqueue($"{name}-end");
            return GenericResult.Success();
        });

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALineAcrossStagesCarriesAllTheRowsAndTheLaterStageStartsAfterTheEarlierOneEnds()
    {
        var log = new ConcurrentQueue<string>();
        var (sink, rows) = Sink("dst");
        var graph = new FlowGraph(
            [Noted("src", 100, log), NotedPass("pass", log), sink],
            [Link("src", "pass"), Link("pass", "dst")],
            [],
            [new FlowStage("src", 1), new FlowStage("pass", 2), new FlowStage("dst", 2)]);

        // Why a buffer of one batch: a bounded line would stop the source on its second batch while the later stage waited for it to finish.
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await _sut.Run(graph, Options(capacity: 1), guard.Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe(Enumerable.Range(1, 100));
        var order = log.ToList();
        order.IndexOf("src-end").ShouldBeLessThan(order.IndexOf("pass-start"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentWithNoStageRunsBeforeStageOneStarts()
    {
        var log = new ConcurrentQueue<string>();
        var (first, firstRows) = Sink("firstDst");
        var (second, secondRows) = Sink("secondDst");
        var graph = new FlowGraph(
            [Noted("first", 20, log), first, Noted("second", 20, log), second],
            [Link("first", "firstDst"), Link("second", "secondDst")],
            [],
            [new FlowStage("second", 1), new FlowStage("secondDst", 1)]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        firstRows.Count.ShouldBe(20);
        secondRows.Count.ShouldBe(20);
        var order = log.ToList();
        order.IndexOf("first-end").ShouldBeLessThan(order.IndexOf("second-start"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALineBackToAnEarlierStageIsRefused()
    {
        var graph = new FlowGraph(
            [Source("src", 3), NotedPass("pass", new ConcurrentQueue<string>()), Sink("dst").Component],
            [Link("src", "pass"), Link("pass", "dst")],
            [],
            [new FlowStage("src", 2), new FlowStage("pass", 1), new FlowStage("dst", 1)]);

        var problems = FlowGraphValidator.Problems(graph);

        problems.ShouldContain(problem => problem.Reason.Contains("runs from stage 2 to stage 1", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALineFromAStageToAComponentWithNoStageIsRefusedBecauseThatOneRunsFirst()
    {
        var graph = new FlowGraph(
            [Source("src", 3), Sink("dst").Component],
            [Link("src", "dst")],
            [],
            [new FlowStage("src", 1)]);

        FlowGraphValidator.Problems(graph).ShouldContain(problem => problem.Reason.Contains("(none)", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStageBelowOneOrOnAComponentThatIsNotThereIsRefused()
    {
        var graph = new FlowGraph(
            [Source("src", 3), Sink("dst").Component],
            [Link("src", "dst")],
            [],
            [new FlowStage("src", 0), new FlowStage("ghost", 1)]);

        var reasons = FlowGraphValidator.Problems(graph).Select(problem => problem.Reason).ToList();

        reasons.ShouldContain(reason => reason.Contains("a number from 1", StringComparison.Ordinal));
        reasons.ShouldContain(reason => reason.Contains("'ghost'", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AGraphWithNoStagesHasNoProblemFromStages()
    {
        var graph = Graph([Source("src", 3), Sink("dst").Component], [Link("src", "dst")]);

        FlowGraphValidator.Problems(graph).ShouldBeEmpty();
    }
}
