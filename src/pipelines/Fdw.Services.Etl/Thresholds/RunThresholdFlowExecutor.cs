using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Wraps the flow executor of one pipeline run so that what each component of a flow took in, passed on, rejected and how long it ran is reported to the
/// threshold evaluator as it finishes (FDW-3865). It tells the progress the caller set, then waits for the reports it queued before it answers.
/// </summary>
public sealed class RunThresholdFlowExecutor : IFlowExecutor
{
    private readonly IFlowExecutor _inner;
    private readonly string _pipelineName;
    private readonly Guid _tenantId;
    private readonly Guid? _runId;
    private readonly IReadOnlyDictionary<string, Guid> _componentIds;
    private readonly IThresholdEvaluator _evaluator;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RunThresholdFlowExecutor"/> class.</summary>
    /// <param name="inner">The executor that runs the flow.</param>
    /// <param name="pipelineName">The pipeline's name.</param>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="runId">The run's identifier, or <see langword="null"/> when the run has none.</param>
    /// <param name="componentIds">The identifier of each component of the graph by its name.</param>
    /// <param name="evaluator">The threshold evaluator.</param>
    /// <param name="logger">The logger.</param>
    public RunThresholdFlowExecutor(
        IFlowExecutor inner, string pipelineName, Guid tenantId, Guid? runId, IReadOnlyDictionary<string, Guid> componentIds, IThresholdEvaluator evaluator, ILogger logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        _pipelineName = pipelineName;
        _tenantId = tenantId;
        _runId = runId;
        _componentIds = componentIds ?? throw new ArgumentNullException(nameof(componentIds));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<FlowRunReport>> Run(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default)
    {
        var outcome = await Execute(graph, options, cancellationToken).ConfigureAwait(false);
        return outcome.Result.IsFailure || outcome.Report is not { } report
            ? outcome.Result.ToNewResult<FlowRunReport>()
            : GenericResult<FlowRunReport>.Success(report);
    }

    /// <inheritdoc/>
    public async Task<FlowRunOutcome> Execute(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);
        using var progress = new RunThresholdProgress(_pipelineName, _tenantId, _runId, _componentIds, _evaluator, options.Progress, _logger);
        try
        {
            return await _inner.Execute(graph, options.WithProgress(progress), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Why drained when the run throws too: the consumer ends only when the queue is completed, and a run that threw would otherwise leave it waiting for ever.
            await progress.Drain().ConfigureAwait(false);
        }
    }
}
