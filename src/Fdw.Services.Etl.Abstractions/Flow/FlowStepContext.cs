using System;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What a step of a pipeline runs against: the variables in scope, the executor, how links buffer, and, for a step
/// that starts another pipeline, the starter and who the run is (FDW-1403).
/// </summary>
public sealed class FlowStepContext
{
    /// <summary>Initializes a new instance of the <see cref="FlowStepContext"/> class.</summary>
    /// <param name="variables">The variables in scope.</param>
    /// <param name="executor">Runs the graph of a stage.</param>
    /// <param name="options">How a stage's links buffer.</param>
    /// <param name="starter">Starts another pipeline; null when the host cannot, which an Execute Pipeline step refuses.</param>
    /// <param name="identity">Who the run is; null when it is untracked, which an Execute Pipeline step refuses.</param>
    public FlowStepContext(
        FlowVariables variables,
        IFlowExecutor executor,
        FlowRunOptions options,
        IPipelineRunStarter? starter,
        FlowRunIdentity? identity)
        : this(variables, executor, options, starter, identity, null, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowStepContext"/> class for a run that records how its steps went.</summary>
    /// <param name="variables">The variables in scope.</param>
    /// <param name="executor">Runs the graph of a stage.</param>
    /// <param name="options">How a stage's links buffer.</param>
    /// <param name="starter">Starts another pipeline; null when the host cannot, which an Execute Pipeline step refuses.</param>
    /// <param name="identity">Who the run is; null when it is untracked, which an Execute Pipeline step refuses.</param>
    /// <param name="reporter">Records how the steps went; null when the run is untracked, which records nothing.</param>
    /// <param name="reportParent">The stage record a step started now is recorded under; null for a step of the run itself.</param>
    public FlowStepContext(
        FlowVariables variables,
        IFlowExecutor executor,
        FlowRunOptions options,
        IPipelineRunStarter? starter,
        FlowRunIdentity? identity,
        IFlowReporter? reporter,
        Guid? reportParent)
    {
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Starter = starter;
        Identity = identity;
        Reporter = reporter;
        ReportParent = reportParent;
    }

    /// <summary>Gets the variables in scope.</summary>
    public FlowVariables Variables { get; }

    /// <summary>Gets the executor that runs a stage's graph.</summary>
    public IFlowExecutor Executor { get; }

    /// <summary>Gets how a stage's links buffer.</summary>
    public FlowRunOptions Options { get; }

    /// <summary>Gets the starter of other pipelines; null when the host has none.</summary>
    public IPipelineRunStarter? Starter { get; }

    /// <summary>Gets who the run is; null when the run is untracked.</summary>
    public FlowRunIdentity? Identity { get; }

    /// <summary>Gets what records how the steps went; null when the run is untracked.</summary>
    public IFlowReporter? Reporter { get; }

    /// <summary>Gets the stage record a step started now is recorded under; null for a step of the run itself.</summary>
    public Guid? ReportParent { get; }

    /// <summary>Makes the context a nested scope sees: the same run with a child variable scope.</summary>
    /// <returns>The child context.</returns>
    public FlowStepContext CreateChild() => new(Variables.CreateChild(), Executor, Options, Starter, Identity, Reporter, ReportParent);

    /// <summary>Makes the context the steps inside a step see: the same scope, recorded under that step's stage.</summary>
    /// <param name="stage">The stage record of the step.</param>
    /// <returns>The context for the step and the steps inside it.</returns>
    public FlowStepContext WithReportParent(Guid stage) => new(Variables, Executor, Options, Starter, Identity, Reporter, stage);
}
