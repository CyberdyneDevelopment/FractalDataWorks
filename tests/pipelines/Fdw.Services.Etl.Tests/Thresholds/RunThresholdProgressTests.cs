using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Thresholds;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for reporting a run's component figures to the threshold evaluator (FDW-3865): a component's figures come from the link counts, error outputs count as rejected
/// and not as rows out, the run time is reported only when the executor says it, a report names the component's subject, and a failing evaluator never fails the run.
/// </summary>
public sealed class RunThresholdProgressTests
{
    private sealed class Component(string name, IReadOnlyList<string> inputs, IReadOnlyList<string> outputs, params string[] errors) : IFlowComponent, IFlowErrorOutputs
    {
        public string Name { get; } = name;

        public IReadOnlyList<string> Inputs { get; } = inputs;

        public IReadOnlyList<string> Outputs { get; } = outputs;

        public IReadOnlyList<string> ErrorOutputs { get; } = errors;

        public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) => Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private sealed class Evaluator(Func<IGenericResult<ThresholdReport>>? answer = null) : IThresholdEvaluator
    {
        public List<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> Reports { get; } = [];

        public Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
        {
            lock (Reports)
            {
                Reports.Add((subject, values));
            }

            return Task.FromResult(answer?.Invoke() ?? GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));
        }

        public Task<IGenericResult<ThresholdReport>> Judge(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Run threshold reporting never judges a held set of thresholds.");

        public IReadOnlyList<ThresholdMatch> Holds(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation) =>
            throw new NotSupportedException("Run threshold reporting never asks which rules hold a consequence.");
    }

    private sealed class Told : IFlowRunProgress
    {
        public List<string> Components { get; } = [];

        public void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links) => Components.Add(component.Name);
    }

    private static readonly Guid Tenant = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Run = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CleanId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private static readonly IReadOnlyDictionary<string, Guid> Ids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
    {
        ["extract"] = Guid.Parse("44444444-4444-4444-8444-444444444441"),
        ["clean"] = CleanId,
        ["write"] = Guid.Parse("44444444-4444-4444-8444-444444444443"),
    };

    private static FlowLinkCount Link(string from, string output, string to, long rows) => new(new FlowLink(from, output, to, "input"), rows, 1, rows * 10);

    // extract -> clean (error output 'rejects') -> write: extract sent 1000, clean passed 940 and rejected 60.
    private static IReadOnlyList<FlowLinkCount> Links() =>
    [
        Link("extract", "output", "clean", 1000),
        Link("clean", "output", "write", 940),
        Link("clean", "rejects", "audit", 60),
    ];

    private static readonly Component Clean = new("clean", ["input"], ["output", "rejects"], "rejects");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentsFiguresAreWhatCameInWhatLeftAndWhatLeftThroughAnErrorOutput()
    {
        var figures = RunThresholdProgress.FiguresOf(Clean, Links(), null);

        figures["rowsIn"].ShouldBe(1000L);
        figures["rowsOut"].ShouldBe(940L);
        figures["rejected"].ShouldBe(60L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunTimeIsLeftOutWhenTheExecutorDoesNotSayIt()
    {
        RunThresholdProgress.FiguresOf(Clean, Links(), null).ContainsKey("runTime").ShouldBeFalse();
        RunThresholdProgress.FiguresOf(Clean, Links(), TimeSpan.FromSeconds(6))["runTime"].ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFinishedComponentIsReportedUnderItsSubjectWithItsFigures()
    {
        var evaluator = new Evaluator();
        using var progress = new RunThresholdProgress("load", Tenant, Run, Ids, evaluator, null);

        progress.ComponentFinished(Clean, Links(), TimeSpan.FromSeconds(6));
        await progress.Drain();

        var report = evaluator.Reports.ShouldHaveSingleItem();
        report.Subject.ShouldBe(new ThresholdSubject("Component", "load/clean", CleanId, Tenant, Run));
        report.Values["rejected"].ShouldBe(60L);
        report.Values["runTime"].ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EveryComponentThatFinishesIsReportedInTheOrderItFinished()
    {
        var evaluator = new Evaluator();
        using var progress = new RunThresholdProgress("load", Tenant, null, Ids, evaluator, null);

        progress.ComponentFinished(new Component("extract", [], ["output"]), Links());
        progress.ComponentFinished(Clean, Links());
        progress.ComponentFinished(new Component("write", ["input"], []), Links());
        await progress.Drain();

        evaluator.Reports.Select(report => report.Subject.Name).ShouldBe(["load/extract", "load/clean", "load/write"]);
        evaluator.Reports.ShouldAllBe(report => report.Subject.RunId == null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheProgressTheCallerSetIsToldToo()
    {
        var told = new Told();
        using var progress = new RunThresholdProgress("load", Tenant, Run, Ids, new Evaluator(), told);

        progress.ComponentFinished(Clean, Links());
        await progress.Drain();

        told.Components.ShouldBe(["clean"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnEvaluatorThatFailsOrThrowsNeverFailsTheRunAndTheNextComponentIsStillReported()
    {
        var calls = 0;
        var evaluator = new Evaluator(() => ++calls == 1 ? throw new InvalidOperationException("source unreachable") : GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));
        using var progress = new RunThresholdProgress("load", Tenant, Run, Ids, evaluator, null);

        progress.ComponentFinished(Clean, Links());
        progress.ComponentFinished(new Component("write", ["input"], []), Links());
        await Should.NotThrowAsync(progress.Drain());

        evaluator.Reports.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentWithNoIdentifierInTheConfigurationIsNotReportedAndTheOthersAre()
    {
        var evaluator = new Evaluator();
        using var progress = new RunThresholdProgress("load", Tenant, Run, Ids, evaluator, null);

        progress.ComponentFinished(new Component("unknown", ["input"], []), Links());
        progress.ComponentFinished(Clean, Links());
        await progress.Drain();

        evaluator.Reports.Select(report => report.Subject.Name).ShouldBe(["load/clean"]);
    }
}
