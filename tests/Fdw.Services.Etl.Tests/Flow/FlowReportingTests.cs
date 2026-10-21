using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for what a run reports (FDW-1602): the executor measures each component, and the steps of a tracked run are reported as
/// stages, a step inside another under it.
/// </summary>
public sealed class FlowReportingTests
{
    private static readonly Guid Outer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private sealed class RecordingReporter : IFlowReporter
    {
        public List<string> Calls { get; } = [];

        public Task<Guid?> StepStarting(string stepName, Guid? parent, CancellationToken cancellationToken = default)
        {
            Calls.Add($"start {stepName} under {(parent is { } p ? (p == Outer ? "outer" : "other") : "run")}");
            return Task.FromResult<Guid?>(stepName == "outer" ? Outer : Guid.NewGuid());
        }

        public Task StepFinished(Guid stage, IGenericResult outcome, CancellationToken cancellationToken = default)
        {
            Calls.Add($"finish {(outcome.IsSuccess ? "ok" : "failed")}");
            return Task.CompletedTask;
        }

        public Task StepCancelled(Guid stage, CancellationToken cancellationToken = default)
        {
            Calls.Add("cancelled");
            return Task.CompletedTask;
        }

        public Task FlowFinished(Guid stage, FlowRunReport report, CancellationToken cancellationToken = default)
        {
            Calls.Add($"flow {report.Components.Count}");
            return Task.CompletedTask;
        }
    }

    // Records each record's name, id and parent, so a test can say which record another is under.
    private sealed class TreeReporter : IFlowReporter
    {
        private readonly object _gate = new();

        public List<(string Name, Guid Id, Guid? Parent)> Started { get; } = [];

        public List<string> Events { get; } = [];

        public Task<Guid?> StepStarting(string stepName, Guid? parent, CancellationToken cancellationToken = default)
        {
            // Why the tracker's rule here: a record the real tracker refuses (FDW-2176: a Stage inside a Stage) is not written and the run goes on, so a test that
            // does not apply the rule sees records the run would never leave.
            if (parent is not null && !ExecutionItemTypes.Stage.CanContain(ExecutionItemTypes.Stage))
            {
                return Task.FromResult<Guid?>(null);
            }

            var id = Guid.NewGuid();
            lock (_gate)
            {
                Started.Add((stepName, id, parent));
            }

            return Task.FromResult<Guid?>(id);
        }

        public Task StepFinished(Guid stage, IGenericResult outcome, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StepCancelled(Guid stage, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task FlowFinished(Guid stage, FlowRunReport report, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                Events.Add($"{Started.Single(s => s.Id == stage).Name}: {string.Join(",", report.Components.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal))}");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubStep : IFlowStep
    {
        private readonly Func<FlowStepContext, CancellationToken, Task<IGenericResult>> _run;

        public StubStep(string name, Func<FlowStepContext, CancellationToken, Task<IGenericResult>> run)
        {
            Name = name;
            _run = run;
        }

        public string Name { get; }

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) => _run(context, cancellationToken);
    }

    private static FlowStepContext Context(IFlowReporter? reporter) =>
        new(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null, reporter, null);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheExecutorReportsWhenEachComponentStartedAndEndedAndThatItSucceeded()
    {
        var (sink, _) = Sink("dst");
        var graph = Graph([Source("src", 6, batchSize: 2), sink], [Link("src", "dst")]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var components = result.Value!.Components;
        components.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(["dst", "src"]);
        components.ShouldAllBe(c => c.Succeeded && c.EndedAt >= c.StartedAt);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentOfALaterStageStartedAfterTheEarlierStageEnded()
    {
        var (first, _) = Sink("first");
        var (second, _) = Sink("second");
        var graph = new FlowGraph(
            [Source("A", 4), Source("B", 4), first, second],
            [Link("A", "first"), Link("B", "second")],
            [],
            [new FlowStage("A", 1), new FlowStage("first", 1), new FlowStage("B", 2), new FlowStage("second", 2)]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var a = result.Value!.Components.Single(c => c.Name == "A");
        var b = result.Value.Components.Single(c => c.Name == "B");
        b.StartedAt.ShouldBeGreaterThanOrEqualTo(a.EndedAt);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachStepOfATrackedRunIsReportedAndAStepInsideAnotherIsReportedUnderIt()
    {
        var reporter = new RecordingReporter();
        var inner = new StubStep("inner", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Success()));
        var outer = new StubStep("outer", (context, token) => new FlowPipeline("body", [inner]).Run(context, token));
        var pipeline = new FlowPipeline("run", [outer]);

        var ran = await pipeline.Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue();
        reporter.Calls.ShouldBe(["start outer under run", "start inner under outer", "finish ok", "finish ok"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailedStepIsReportedFailedAndTheRunIsStillFailed()
    {
        var reporter = new RecordingReporter();
        var failing = new StubStep("load", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Failure(new Fdw.Messages.GenericMessage("no"))));

        var ran = await new FlowPipeline("run", [failing]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        reporter.Calls.ShouldBe(["start load under run", "finish failed"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFlowThatFailsIsStillReportedWithItsComponentsAndThenFailsTheStep()
    {
        var reporter = new RecordingReporter();
        var failing = new ScriptedComponent(
            "boom", ["input"], ["output"], (_, _) => Task.FromResult<IGenericResult>(GenericResult.Failure(new Fdw.Messages.GenericMessage("no"))));
        var (sink, _) = Sink("dst");
        var graph = Graph([Source("src", 6), failing, sink], [Link("src", "boom"), Link("boom", "dst")]);

        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        reporter.Calls.ShouldBe(["start No stage under run", "flow 3", "finish failed"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatIsNotTrackedReportsNothingAndRunsAsBefore()
    {
        var ran = await new FlowPipeline("run", [new StubStep("load", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Success()))])
            .Run(Context(null), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADataFlowStepHandsItsReportToTheReporterUnderItsStageRecord()
    {
        var reporter = new RecordingReporter();
        var (sink, _) = Sink("dst");
        var step = new FlowStageStep("flow", Graph([Source("src", 4), sink], [Link("src", "dst")]));

        var ran = await new FlowPipeline("run", [step]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Calls.ShouldBe(["start No stage under run", "flow 2", "finish ok"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALevelIsRecordedAsOneRecordForEachStageNumberWithTheEventsOfItsComponents()
    {
        var reporter = new TreeReporter();
        var (first, _) = Sink("first");
        var (second, _) = Sink("second");
        var graph = new FlowGraph(
            [Source("A", 4), Source("B", 4), first, second],
            [Link("A", "first"), Link("B", "second")],
            [],
            [new FlowStage("A", 1), new FlowStage("first", 1), new FlowStage("B", 2), new FlowStage("second", 2)]);

        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Started.Select(s => s.Name).ShouldBe(["Stage 1", "Stage 2"]);
        reporter.Started.ShouldAllBe(s => s.Parent == null);
        reporter.Events.ShouldBe(["Stage 1: A,first", "Stage 2: B,second"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ComponentsWithNoStageAreTheRecordNoStageAndAnActionIsRecordedUnderItsOwnStage()
    {
        var reporter = new TreeReporter();
        var (sink, _) = Sink("dst");
        var action = new FlowActionComponent(new StubStep("act", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Success())));
        var graph = new FlowGraph([Source("src", 4), sink, action], [Link("src", "dst")], [], [new FlowStage("act", 1)]);

        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Started.Select(s => s.Name).ShouldBe(["No stage", "Stage 1", "act"]);
        var stageOne = reporter.Started.Single(s => s.Name == "Stage 1").Id;
        reporter.Started.Single(s => s.Name == "act").Parent.ShouldBe(stageOne);
        reporter.Events.ShouldBe(["No stage: dst,src", "Stage 1: act"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARoundOfALoopIsNumberedUnderTheLoopAndNotNamedForIt()
    {
        var reporter = new TreeReporter();
        var inner = new StubStep("inner", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Success()));
        var loop = new FlowForLoop("loop", "i", 1, 2, 1, [inner]);

        var ran = await new FlowPipeline("run", [loop]).Run(Context(reporter), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Started.Select(s => s.Name).ShouldBe(["loop", "Round 1", "inner", "Round 2", "inner"]);
        var loopId = reporter.Started.Single(s => s.Name == "loop").Id;
        reporter.Started.Where(s => s.Name.StartsWith("Round", StringComparison.Ordinal)).ShouldAllBe(s => s.Parent == loopId);
        reporter.Started.Where(s => s.Name == "inner").Select(s => s.Parent).ShouldBe(reporter.Started.Where(s => s.Name.StartsWith("Round", StringComparison.Ordinal)).Select(s => (Guid?)s.Id));
    }
}
