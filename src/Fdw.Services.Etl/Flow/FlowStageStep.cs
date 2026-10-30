using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A level of a pipeline: a graph of components that run in their stages and all finish before the step ends (FDW-1403, FDW-2041). The top
/// level is the pipeline's own; the body of a loop is another, run again each round.
/// </summary>
/// <remarks>
/// <para>
/// A component of the graph that is an action or a loop (see <see cref="FlowActionComponent"/>) runs as a step of the flow, so the step gives
/// each such component what it runs against before the graph starts, and it passes a failure on to the actions that fire on one.
/// </para>
/// <para>
/// A tracked run records the level as one record for each stage number (FDW-2060), "Stage 1", "Stage 2", and "No stage" for the components that have
/// none, each opened when its stage starts and holding the events of its components; the level itself has no record named after it.
/// </para>
/// </remarks>
public sealed class FlowStageStep : IFlowStep, IFlowFailureHook
{
    private readonly FlowGraph _graph;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowStageStep"/> class.</summary>
    /// <param name="name">The stage's name.</param>
    /// <param name="graph">The stage's components and links.</param>
    /// <param name="logger">The logger.</param>
    public FlowStageStep(string name, FlowGraph graph, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var bound in _graph.Components.OfType<IFlowStepBound>())
        {
            bound.Bind(context);
        }

        var records = context.Reporter is { } reporter ? new FlowStageRecords(reporter, context.ReportParent) : null;
        if (records is not null)
        {
            foreach (var component in _graph.Components)
            {
                if (component is IFlowStageBound recorded)
                {
                    recorded.BindStages(records, _graph.RankOf(component.Name));
                }
            }
        }

        var options = records is null
            ? context.Options
            : context.Options.WithStages(records);
        var ran = await context.Executor.Execute(_graph, options, cancellationToken).ConfigureAwait(false);

        // Why a failed flow is recorded too, and without the run's token: a run that was stopped still did something, and the record of
        // which component failed and how far the rows got is the point of recording it (FDW-1691).
        if (records is not null && ran.Report is { } report)
        {
            await records.Finish(report, ran.Result, ran.Result.IsFailure ? CancellationToken.None : cancellationToken).ConfigureAwait(false);
        }

        if (ran.Result.IsFailure)
        {
            return ran.Result;
        }

        FlowLog.StepFinished(_logger, Name);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>The hooks of the actions this level holds directly; a loop's body is not reached, as a Notify on failure sits at the top of the pipeline.</remarks>
    public async Task<IGenericResult> OnFailure(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken = default)
    {
        foreach (var hook in _graph.Components.OfType<FlowActionComponent>().Select(action => action.Step).OfType<IFlowFailureHook>())
        {
            _ = await hook.OnFailure(context, failure, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }
}
