using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for the executor that reports a flow's components to the threshold evaluator (FDW-3865): the progress the executor runs the flow with reports every component that
/// finishes, the caller's own progress is told too, and the executor does not answer before the reports are made.
/// </summary>
public sealed class RunThresholdFlowExecutorTests
{
    private sealed class Component(string name) : IFlowComponent
    {
        public string Name { get; } = name;

        public IReadOnlyList<string> Inputs { get; } = [];

        public IReadOnlyList<string> Outputs { get; } = [];

        public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) => Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private sealed class FinishesComponents(params IFlowComponent[] components) : IFlowExecutor
    {
        public Task<IGenericResult<FlowRunReport>> Run(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FlowRunOutcome> Execute(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default)
        {
            foreach (var component in components)
            {
                (options.Progress as IFlowRunTimedProgress)!.ComponentFinished(component, [], TimeSpan.FromMilliseconds(5));
            }

            return Task.FromResult(new FlowRunOutcome(GenericResult.Success(), null));
        }
    }

    private sealed class SlowEvaluator : IThresholdEvaluator
    {
        public List<string> Reported { get; } = [];

        public async Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
        {
            await Task.Delay(30, cancellationToken);
            lock (Reported)
            {
                Reported.Add(subject.Name);
            }

            return GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], []));
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

    private static readonly IReadOnlyDictionary<string, Guid> Ids = new Dictionary<string, Guid>
    {
        ["a"] = Guid.Parse("55555555-5555-4555-8555-555555555551"),
        ["b"] = Guid.Parse("55555555-5555-4555-8555-555555555552"),
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EveryComponentThatFinishesIsReportedBeforeTheExecutorAnswers()
    {
        var evaluator = new SlowEvaluator();
        var executor = new RunThresholdFlowExecutor(
            new FinishesComponents(new Component("a"), new Component("b")), "load", Guid.NewGuid(), Guid.NewGuid(), Ids, evaluator, NullLogger.Instance);

        var outcome = await executor.Execute(new FlowGraph([], [], []), new FlowRunOptions { LinkCapacity = 2 }, TestContext.Current.CancellationToken);

        outcome.Result.IsSuccess.ShouldBeTrue();
        evaluator.Reported.ShouldBe(["load/a", "load/b"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheCallersOwnProgressIsToldToo()
    {
        var told = new Told();
        var executor = new RunThresholdFlowExecutor(
            new FinishesComponents(new Component("a")), "load", Guid.NewGuid(), null, Ids, new SlowEvaluator(), NullLogger.Instance);

        await executor.Execute(new FlowGraph([], [], []), new FlowRunOptions { LinkCapacity = 2, Progress = told }, TestContext.Current.CancellationToken);

        told.Components.ShouldBe(["a"]);
    }
}
