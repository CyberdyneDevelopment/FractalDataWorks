using System.Collections.Concurrent;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for completion lines (FDW-3640): a component waits until the component at the start of its completion line has finished, whatever their stages, and the
/// run refuses a completion line that could never be satisfied.
/// </summary>
public sealed class FlowCompletionTests
{
    private readonly FlowExecutor _sut = new();

    private static FlowSource Noted(string name, ConcurrentQueue<string> log) =>
        new(name, ct => NotedRows(name, log, ct), 2);

    private static async IAsyncEnumerable<IDictionary<string, object?>> NotedRows(
        string name, ConcurrentQueue<string> log, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        log.Enqueue($"{name}-start");
        for (var i = 1; i <= 6; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Row(i);
            await Task.Yield();
        }
    }

    // A destination that notes when it has taken its last row.
    private static ScriptedComponent NotedEnd(string name, ConcurrentQueue<string> log) =>
        new(name, ["input"], [], async (ports, ct) =>
        {
            var input = ports.Input("input");
            while (await input.Read(ct) is not null)
            {
                await Task.Yield();
            }

            log.Enqueue($"{name}-end");
            return GenericResult.Success();
        });

    private static FlowGraph TwoChains(ConcurrentQueue<string> log, params FlowCompletion[] completions) =>
        new(
            [Noted("srcA", log), NotedEnd("dstA", log), Noted("srcB", log), NotedEnd("dstB", log)],
            [Link("srcA", "dstA", "output", "input"), Link("srcB", "dstB", "output", "input")],
            [],
            [],
            completions);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentWithACompletionLineStartsOnlyAfterTheComponentAtItsStartHasFinished()
    {
        var log = new ConcurrentQueue<string>();
        var graph = TwoChains(log, new FlowCompletion("dstA", "srcB"));

        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await _sut.Run(graph, Options(), guard.Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var order = log.ToList();
        order.IndexOf("dstA-end").ShouldBeGreaterThanOrEqualTo(0);
        order.IndexOf("dstA-end").ShouldBeLessThan(order.IndexOf("srcB-start"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AGraphWithCompletionLinesKeepsThemAndAGraphBuiltWithoutThemHasNone()
    {
        var log = new ConcurrentQueue<string>();

        TwoChains(log, new FlowCompletion("dstA", "srcB")).Completions.ShouldBe([new FlowCompletion("dstA", "srcB")]);
        TwoChains(log).Completions.ShouldBeEmpty();
        Graph([Noted("src", log), NotedEnd("dst", log)], [Link("src", "dst")]).Completions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACompletionLineBetweenComponentsThatPassRowsToEachOtherIsRefused()
    {
        var log = new ConcurrentQueue<string>();
        var graph = new FlowGraph(
            [Noted("src", log), NotedEnd("dst", log)], [Link("src", "dst")], [], [], [new FlowCompletion("dst", "src")]);

        var problems = FlowGraphValidator.Problems(graph);

        problems.ShouldContain(problem => problem.NodeKind == "completion" && problem.Reason.Contains("run together", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACompletionLineToAnEarlierStageIsRefusedAndOneToALaterStageIsNot()
    {
        var log = new ConcurrentQueue<string>();
        var forward = new FlowGraph(
            [Noted("srcA", log), NotedEnd("dstA", log), Noted("srcB", log), NotedEnd("dstB", log)],
            [Link("srcA", "dstA"), Link("srcB", "dstB")],
            [],
            [new FlowStage("srcA", 1), new FlowStage("dstA", 1), new FlowStage("srcB", 2), new FlowStage("dstB", 2)],
            [new FlowCompletion("dstA", "srcB")]);
        var back = new FlowGraph(
            [Noted("srcA", log), NotedEnd("dstA", log), Noted("srcB", log), NotedEnd("dstB", log)],
            [Link("srcA", "dstA"), Link("srcB", "dstB")],
            [],
            [new FlowStage("srcA", 1), new FlowStage("dstA", 1), new FlowStage("srcB", 2), new FlowStage("dstB", 2)],
            [new FlowCompletion("dstB", "srcA")]);

        FlowGraphValidator.Problems(forward).ShouldBeEmpty();
        FlowGraphValidator.Problems(back).ShouldContain(problem => problem.Reason.Contains("runs from stage 2 to stage 1", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void CompletionLinesThatWaitForEachOtherAreRefusedAsACycle()
    {
        var log = new ConcurrentQueue<string>();
        var graph = TwoChains(log, new FlowCompletion("dstA", "srcB"), new FlowCompletion("dstB", "srcA"));

        FlowGraphValidator.Problems(graph).ShouldContain(problem => problem.Reason.Contains("form a cycle", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACompletionLineToItselfOrGivenTwiceOrToAMissingComponentIsRefused()
    {
        var log = new ConcurrentQueue<string>();

        FlowGraphValidator.Problems(TwoChains(log, new FlowCompletion("dstA", "dstA")))
            .ShouldContain(problem => problem.Reason.Contains("wait for itself", StringComparison.Ordinal));
        FlowGraphValidator.Problems(TwoChains(log, new FlowCompletion("dstA", "srcB"), new FlowCompletion("DSTA", "SRCB")))
            .ShouldContain(problem => problem.Reason.Contains("given twice", StringComparison.Ordinal));
        FlowGraphValidator.Problems(TwoChains(log, new FlowCompletion("dstA", "nowhere")))
            .ShouldContain(problem => problem.Reason.Contains("does not exist", StringComparison.Ordinal));
    }
}
