using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for <see cref="FlowExecutor"/> (FDW-1386): rows move between components on bounded channels, are routed by output node
/// filters, are copied for every link of an output, and a failure or a cancellation stops the whole run.
/// </summary>
public sealed class FlowExecutorTests
{
    private readonly FlowExecutor _sut = new();

    private static Task<IGenericResult> Done() => Task.FromResult<IGenericResult>(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RowsMoveFromASourceThroughAMappedInputToADestinationInOrder()
    {
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [
                Source("src", 10, batchSize: 3),
                Mapped("map", FlowFieldMap.From("Key", "Id"), FlowFieldMap.Of("Origin", "test")),
                sink
            ],
            [Link("src", "map"), Link("map", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(r => (int)r["Key"]!).ShouldBe(Enumerable.Range(1, 10));
        rows.ShouldAllBe(r => (string)r["Origin"]! == "test" && !r.ContainsKey("Id"));
        result.Value!.Links.Select(l => l.Rows).ShouldBe(new long[] { 10, 10 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFirstMatchSplitSendsEachRowDownTheFirstFilterItMeetsAndTheRestToTheOtherOutput()
    {
        var (a, aRows) = Sink("a");
        var (big, bigRows) = Sink("big");
        var (rest, restRows) = Sink("rest");
        var split = new FlowConditionalSplit(
            "split",
            [
                new FlowSplitOutput("regionA", r => (string)r["Region"]! == "A"),
                new FlowSplitOutput("big", r => (int)r["Id"]! > 4)
            ],
            SplitRoutings.ByName("FirstMatch"),
            FlowUnmatched.SendTo("rest"));
        var graph = Graph(
            [Source("src", 10, region: i => i % 2 == 0 ? "A" : "B"), split, a, big, rest],
            [Link("src", "split"), Link("split", "a", output: "regionA"), Link("split", "big", output: "big"), Link("split", "rest", output: "rest")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(aRows).OrderBy(i => i).ShouldBe(new[] { 2, 4, 6, 8, 10 });
        Ids(bigRows).OrderBy(i => i).ShouldBe(new[] { 5, 7, 9 });
        Ids(restRows).OrderBy(i => i).ShouldBe(new[] { 1, 3 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAllMatchesSplitSendsARowThroughEveryFilterItMeets()
    {
        var (low, lowRows) = Sink("low");
        var (even, evenRows) = Sink("even");
        var split = new FlowConditionalSplit(
            "split",
            [
                new FlowSplitOutput("low", r => (int)r["Id"]! <= 3),
                new FlowSplitOutput("even", r => (int)r["Id"]! % 2 == 0)
            ],
            SplitRoutings.ByName("AllMatches"),
            FlowUnmatched.SendTo("other"));
        var graph = Graph(
            [Source("src", 6), split, low, even],
            [Link("src", "split"), Link("split", "low", output: "low"), Link("split", "even", output: "even")],
            new FlowDroppedOutput("split", "other"));

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(lowRows).OrderBy(i => i).ShouldBe(new[] { 1, 2, 3 });
        Ids(evenRows).OrderBy(i => i).ShouldBe(new[] { 2, 4, 6 });
        result.Value!.Links.Single(l => l.Link.FromOutput == "other").Rows.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARowThatMeetsNoFilterOfASplitSetToFailStopsTheRunAndNamesTheSplit()
    {
        var (sink, _) = Sink("dst");
        var split = new FlowConditionalSplit(
            "split", [new FlowSplitOutput("only", r => (int)r["Id"]! < 3)], SplitRoutings.ByName("FirstMatch"), FlowUnmatched.Fail);
        var graph = Graph([Source("src", 10), split, sink], [Link("src", "split"), Link("split", "dst", output: "only")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("split");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OneOutputLinkedToTwoInputsSendsEachAnIndependentCopy()
    {
        var (first, firstRows) = Sink("first");
        var edited = new FlowDestination("second", (batch, _) =>
        {
            foreach (var row in batch.Rows)
            {
                row["Id"] = -1;
            }

            return Done();
        });
        var graph = Graph([Source("src", 6), first, edited], [Link("src", "first"), Link("src", "second")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(firstRows).OrderBy(i => i).ShouldBe(Enumerable.Range(1, 6));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TwoLinksIntoOneInputMergeTheirRowsAndKeepEachSourcesOrder()
    {
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [Source("one", 5, region: _ => "one"), Source("two", 5, region: _ => "two"), sink],
            [Link("one", "dst"), Link("two", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(10);
        Ids(rows.Where(r => (string)r["Region"]! == "one")).ShouldBe(Enumerable.Range(1, 5));
        Ids(rows.Where(r => (string)r["Region"]! == "two")).ShouldBe(Enumerable.Range(1, 5));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AUnionAllMapsEachInputOntoTheUnifiedFields()
    {
        var (sink, rows) = Sink("dst");
        var union = new FlowMappedComponent(
            new FlowUnionAll("union", ["left", "right"]),
            new Dictionary<string, IReadOnlyList<FlowFieldMap>>
            {
                ["left"] = [FlowFieldMap.From("Key", "Id"), FlowFieldMap.Of("Side", "L")],
                ["right"] = [FlowFieldMap.From("Key", "Id"), FlowFieldMap.Of("Side", "R")]
            });
        var graph = Graph(
            [Source("a", 3), Source("b", 2), union, sink],
            [Link("a", "union", input: "left"), Link("b", "union", input: "right"), Link("union", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count(r => (string)r["Side"]! == "L").ShouldBe(3);
        rows.Count(r => (string)r["Side"]! == "R").ShouldBe(2);
        rows.ShouldAllBe(r => r.ContainsKey("Key") && !r.ContainsKey("Id"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailureInOneComponentStopsTheSourceAndIsTheRunsResult()
    {
        var produced = 0;
        var source = new FlowSource("src", ct => Counted(100_000, () => Interlocked.Increment(ref produced), ct), 10);
        var writes = 0;
        var failing = new FlowDestination("dst", (_, _) =>
            Interlocked.Increment(ref writes) >= 2
                ? Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("the table refused the batch")))
                : Done());
        var graph = Graph([source, failing], [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(capacity: 1), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("dst");
        result.CurrentMessage.ShouldContain("the table refused the batch");
        produced.ShouldBeLessThan(1_000);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AComponentThatThrowsFailsTheRunNamingItAndTheExceptionType()
    {
        var thrower = new ScriptedComponent(
            "boom", ["input"], ["output"], (_, _) => throw new InvalidOperationException("kaput"));
        var (sink, _) = Sink("dst");
        var graph = Graph([Source("src", 100), thrower, sink], [Link("src", "boom"), Link("boom", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("boom");
        result.CurrentMessage.ShouldContain("InvalidOperationException");
        result.CurrentMessage.ShouldContain("kaput");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailedRunStillHandsBackItsReportNamingTheComponentThatFailed()
    {
        var failing = new ScriptedComponent(
            "boom", ["input"], ["output"], (_, _) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("a record failed"))));
        var (sink, _) = Sink("dst");
        var graph = Graph([Source("src", 100), failing, sink], [Link("src", "boom"), Link("boom", "dst")]);

        var outcome = await _sut.Execute(graph, Options(), TestContext.Current.CancellationToken);

        outcome.Result.IsFailure.ShouldBeTrue();
        var report = outcome.Report.ShouldNotBeNull();
        report.FailedComponent.ShouldBe("boom");
        report.Components.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(["boom", "dst", "src"]);
        report.Components.Single(c => c.Name == "boom").Succeeded.ShouldBeFalse();
        report.Links.Select(l => l.Link.FromComponent).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(["boom", "src"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASucceededRunNamesNoFailedComponent()
    {
        var (sink, _) = Sink("dst");

        var outcome = await _sut.Execute(Graph([Source("src", 6), sink], [Link("src", "dst")]), Options(), TestContext.Current.CancellationToken);

        outcome.Result.IsSuccess.ShouldBeTrue();
        outcome.Report.ShouldNotBeNull().FailedComponent.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunRefusedBeforeItStartsHasNoReport()
    {
        var (sink, _) = Sink("dst");

        var outcome = await _sut.Execute(Graph([Source("src", 6), sink], [Link("src", "dst")]), Options(capacity: 0), TestContext.Current.CancellationToken);

        outcome.Result.IsFailure.ShouldBeTrue();
        outcome.Report.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACancelledRunEndsPromptlyAndReportsCancellation()
    {
        var source = new FlowSource("src", ct => Counted(int.MaxValue, () => 0, ct), 10);
        var (sink, _) = Sink("dst");
        using var cancel = new CancellationTokenSource();
        var run = _sut.Run(Graph([source, sink], [Link("src", "dst")]), Options(), cancel.Token);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        cancel.Cancel();
        var finished = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        finished.ShouldBe(run);
        (await run).IsFailure.ShouldBeTrue();
        (await run).CurrentMessage.ShouldNotBeNull().ShouldContain("cancelled");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASlowDestinationHoldsASourceBackToTheLinksCapacity()
    {
        var produced = 0;
        var consumed = 0;
        var maxLead = 0;
        var source = new FlowSource("src", ct => Counted(200, () => Interlocked.Increment(ref produced), ct), 1);
        var slow = new FlowDestination("dst", async (batch, ct) =>
        {
            await Task.Delay(2, ct);
            var lead = Volatile.Read(ref produced) - Interlocked.Add(ref consumed, batch.Count);
            var seen = Volatile.Read(ref maxLead);
            while (lead > seen && Interlocked.CompareExchange(ref maxLead, lead, seen) != seen)
            {
                seen = Volatile.Read(ref maxLead);
            }

            return GenericResult.Success();
        });

        var result = await _sut.Run(Graph([source, slow], [Link("src", "dst")]), Options(capacity: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        consumed.ShouldBe(200);
        // capacity 2 batches of 1 row, plus the row in the writer's hand and the one being read: a handful, never the 200.
        maxLead.ShouldBeLessThanOrEqualTo(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGraphThatCannotRunIsRefusedBeforeAnyComponentStarts()
    {
        var started = false;
        var spy = new ScriptedComponent("spy", [], ["output"], (_, _) =>
        {
            started = true;
            return Done();
        });
        var graph = Graph([spy], []);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        started.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALinkCapacityBelowOneIsRefused()
    {
        var (sink, _) = Sink("dst");

        var result = await _sut.Run(
            Graph([Source("src", 1), sink], [Link("src", "dst")]), new FlowRunOptions { LinkCapacity = 0 }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("at least one batch");
    }

    private static async IAsyncEnumerable<IDictionary<string, object?>> Counted(
        int count, Func<int> onYield, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onYield();
            yield return Row(i);
            await Task.Yield();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APausedRunPassesNoRowsOnUntilItIsResumed()
    {
        var (sink, rows) = Sink("dst");
        var graph = Graph([Source("src", 10, batchSize: 3), sink], [Link("src", "dst")]);
        var gate = new PipelineRunPauseGate();
        gate.Pause().ShouldBeTrue();

        var running = _sut.Run(graph, new FlowRunOptions { LinkCapacity = 2, PauseGate = gate }, TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);

        running.IsCompleted.ShouldBeFalse();
        rows.Count.ShouldBe(0);

        gate.Resume().ShouldBeTrue();
        var result = await running;
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACancelledPausedRunEndsInsteadOfWaitingForever()
    {
        var (sink, _) = Sink("dst");
        var graph = Graph([Source("src", 10, batchSize: 3), sink], [Link("src", "dst")]);
        var gate = new PipelineRunPauseGate();
        gate.Pause().ShouldBeTrue();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var running = _sut.Run(graph, new FlowRunOptions { LinkCapacity = 2, PauseGate = gate }, cancel.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cancel.CancelAsync();

        var result = await running.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        result.IsFailure.ShouldBeTrue();
    }
}
