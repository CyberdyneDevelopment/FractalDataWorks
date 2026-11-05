using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Reports what each component of a run took in, passed on, rejected and how long it ran, to the threshold evaluator, as the components finish (FDW-3865). The
/// figures are read from the link counts; the report is queued and made by one consumer, so a component finishing never waits on a threshold's consequence. One
/// progress serves one run.
/// </summary>
public sealed class RunThresholdProgress : IFlowRunTimedProgress, IDisposable
{
    private readonly string _pipelineName;
    private readonly Guid _tenantId;
    private readonly Guid? _runId;
    private readonly IReadOnlyDictionary<string, Guid> _componentIds;
    private readonly IFlowRunProgress? _inner;
    private readonly ILogger _logger;
    private readonly Channel<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> _reports = Channel.CreateUnbounded<(ThresholdSubject, IReadOnlyDictionary<string, object?>)>(
        new UnboundedChannelOptions { SingleReader = true });
    private readonly SemaphoreSlim _consumed = new(0, 1);

    /// <summary>Initializes a new instance of the <see cref="RunThresholdProgress"/> class.</summary>
    /// <param name="pipelineName">The pipeline's name.</param>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="runId">The run's identifier, or <see langword="null"/> when the run has none.</param>
    /// <param name="componentIds">The identifier of each component of the graph by its name, as the run's configuration holds them; a threshold is attached to a component by it.</param>
    /// <param name="evaluator">The threshold evaluator.</param>
    /// <param name="inner">The progress that is told too, or <see langword="null"/>.</param>
    /// <param name="logger">The logger.</param>
    public RunThresholdProgress(
        string pipelineName, Guid tenantId, Guid? runId, IReadOnlyDictionary<string, Guid> componentIds, IThresholdEvaluator evaluator, IFlowRunProgress? inner, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(componentIds);
        ArgumentNullException.ThrowIfNull(evaluator);
        _pipelineName = pipelineName;
        _componentIds = componentIds;
        _tenantId = tenantId;
        _runId = runId;
        _inner = inner;
        _logger = logger ?? NullLogger.Instance;
        _ = Consume(evaluator);
    }

    /// <inheritdoc/>
    public void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links) => Finished(component, links, null);

    /// <inheritdoc/>
    public void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links, TimeSpan runTime) => Finished(component, links, runTime);

    /// <summary>Waits for every queued report to be made; the executor calls it when the run has ended.</summary>
    /// <returns>A task that completes when the queue is empty.</returns>
    public async Task Drain()
    {
        _reports.Writer.TryComplete();
        await _consumed.WaitAsync().ConfigureAwait(false);

        // Why released again: a second Drain finds the consumer already finished and must not wait for it.
        _ = _consumed.Release();
    }

    /// <summary>Reads what a component took in, passed on and rejected from the link counts of the flow.</summary>
    /// <param name="component">The component.</param>
    /// <param name="links">The count of every link in the flow.</param>
    /// <param name="runTime">How long it ran, or <see langword="null"/> when the executor did not say.</param>
    /// <returns>The figures, named as <see cref="ComponentIoThresholdType"/> names them; <c>runTime</c> is left out when it is not known.</returns>
    public static IReadOnlyDictionary<string, object?> FiguresOf(IFlowComponent component, IReadOnlyList<FlowLinkCount> links, TimeSpan? runTime)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(links);
        var errors = component is IFlowErrorOutputs outputs ? outputs.ErrorOutputs : [];
        var from = links.Where(link => Same(link.Link.FromComponent, component.Name)).ToList();
        var rejected = from.Where(link => errors.Contains(link.Link.FromOutput, StringComparer.OrdinalIgnoreCase)).Sum(link => link.Rows);
        var figures = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [ComponentIoThresholdType.RowsIn] = links.Where(link => Same(link.Link.ToComponent, component.Name)).Sum(link => link.Rows),
            [ComponentIoThresholdType.RowsOut] = from.Sum(link => link.Rows) - rejected,
            [ComponentIoThresholdType.Rejected] = rejected,
        };
        if (runTime is { } time)
        {
            figures[ComponentIoThresholdType.RunTime] = time;
        }

        return figures;
    }

    private void Finished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links, TimeSpan? runTime)
    {
        if (_inner is IFlowRunTimedProgress timed && runTime is { } time)
        {
            timed.ComponentFinished(component, links, time);
        }
        else
        {
            _inner?.ComponentFinished(component, links);
        }

        // Why a component with no identifier is not reported: a threshold is attached to a component through its identifier, so figures without one belong to
        // no threshold. It is logged as an error and not guessed from the name (FDW-3865).
        if (!_componentIds.TryGetValue(component.Name, out var componentId) || componentId == Guid.Empty)
        {
            RunThresholdLog.ComponentHasNoId(_logger, _pipelineName, component.Name);
            return;
        }

        _reports.Writer.TryWrite((
            new ThresholdSubject(ComponentIoThresholdType.Subject, string.Concat(_pipelineName, "/", component.Name), componentId, _tenantId, _runId),
            FiguresOf(component, links, runTime)));
    }

    private async Task Consume(IThresholdEvaluator evaluator)
    {
        try
        {
            await foreach (var (subject, values) in _reports.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    var reported = await evaluator.Report(subject, values).ConfigureAwait(false);
                    if (reported.IsFailure)
                    {
                        RunThresholdLog.ReportFailed(_logger, subject.Name, reported.CurrentMessage ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    RunThresholdLog.ReportFailed(_logger, subject.Name, ex.Message);
                }
            }
        }
        finally
        {
            // Why a semaphore and not the consumer's task: Drain awaits the end of a loop that started in the constructor, and awaiting a stored task is what VSTHRD003 refuses.
            _ = _consumed.Release();
        }
    }

    /// <summary>Disposes the semaphore <see cref="Drain"/> waits on; call it after <see cref="Drain"/> has returned.</summary>
    public void Dispose() => _consumed.Dispose();

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
