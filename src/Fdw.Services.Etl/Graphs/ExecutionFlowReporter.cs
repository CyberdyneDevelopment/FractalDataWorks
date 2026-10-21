using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Records how the steps of a tracked run went as child executions of the run (FDW-1602): one stage record for each stage number of a level
/// (FDW-2060), one for each action or loop and one for each round of a loop, and for a stage one event for each component and one for each link.
/// </summary>
/// <remarks>
/// <para>
/// A stage is an execution of type Stage named for what it is: a stage number ("Stage 1", "No stage"), a round of a loop ("Round 2") or an action
/// or loop by its component's name, under the run (or under the record of the step it runs in: an action's under its stage's). It goes
/// Running when it starts and Completed or Failed when it ends. A stage of a data flow carries the events
/// <see cref="ComponentEvent"/> (data: <c>component</c>, <c>startedAt</c>, <c>endedAt</c>, <c>succeeded</c>, <c>rowsOut</c>) and
/// <see cref="LinkEvent"/> (data: <c>from</c>, <c>fromOutput</c>, <c>to</c>, <c>toInput</c>, <c>rows</c>, <c>batches</c>), read with the
/// execution endpoints <c>/executions/{id}/children</c> and <c>/executions/{id}/events</c>.
/// </para>
/// <para>
/// A record that cannot be written is logged and the run goes on: the report observes the run, it is not part of it.
/// </para>
/// </remarks>
public sealed class ExecutionFlowReporter : IFlowReporter
{
    /// <summary>The event type of a component that finished, one for each component of a data flow.</summary>
    public const string ComponentEvent = "component.finished";

    /// <summary>The event type of a link, one for each link of a data flow.</summary>
    public const string LinkEvent = "link.finished";

    /// <summary>The event type of the rows one error output carried (FDW-1720).</summary>
    public const string RejectedRowsEvent = "rows.rejected";

    private readonly IExecutionTracker _tracker;
    private readonly Guid _runId;
    private readonly Guid? _tenantId;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ExecutionFlowReporter"/> class.</summary>
    /// <param name="tracker">Writes the executions.</param>
    /// <param name="runId">The execution the run is tracked as; the stages of its top-level steps are its children.</param>
    /// <param name="tenantId">The tenant of the run.</param>
    /// <param name="logger">The logger.</param>
    public ExecutionFlowReporter(IExecutionTracker tracker, Guid runId, Guid? tenantId, ILogger? logger = null)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _runId = runId;
        _tenantId = tenantId;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public async Task<Guid?> StepStarting(string stepName, Guid? parent, CancellationToken cancellationToken = default)
    {
        var created = await _tracker
            .CreateItem(ExecutionItemTypes.Stage, stepName, _tenantId, parent.HasValue ? parent.Value : _runId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (created.IsFailure || created.Value is not { } item)
        {
            GraphLog.ReportNotWritten(_logger, stepName, created.CurrentMessage);
            return null;
        }

        // Why the three moves: an execution is created Scheduled and reaches Running through Triggered and Initialized, as a run does.
        foreach (var state in new IExecutionStateType[] { ExecutionStateTypes.Triggered, ExecutionStateTypes.Initialized, ExecutionStateTypes.Running })
        {
            var moved = await _tracker.TransitionState(item.Id, state, $"Step '{stepName}' {state.Name}", "Flow", cancellationToken).ConfigureAwait(false);
            if (moved.IsFailure)
            {
                GraphLog.ReportNotWritten(_logger, stepName, moved.CurrentMessage);
                return null;
            }
        }

        return item.Id;
    }

    /// <inheritdoc />
    public async Task StepFinished(Guid stage, IGenericResult outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var completed = await _tracker
            .Complete(stage, outcome.IsSuccess, outcome.IsSuccess ? "Completed" : "Failed", outcome.CurrentMessage, cancellationToken)
            .ConfigureAwait(false);
        if (completed.IsFailure)
        {
            GraphLog.ReportNotWritten(_logger, stage.ToString(), completed.CurrentMessage);
        }
    }

    /// <inheritdoc />
    public async Task StepCancelled(Guid stage, CancellationToken cancellationToken = default)
    {
        var completed = await _tracker.Complete(stage, false, "Cancelled", "The run was cancelled", cancellationToken).ConfigureAwait(false);
        if (completed.IsFailure)
        {
            GraphLog.ReportNotWritten(_logger, stage.ToString(), completed.CurrentMessage);
        }
    }

    /// <inheritdoc />
    public async Task FlowFinished(Guid stage, FlowRunReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        foreach (var component in report.Components.OrderBy(c => c.StartedAt).ThenBy(c => c.Name, StringComparer.Ordinal))
        {
            var rowsOut = report.Links
                .Where(l => string.Equals(l.Link.FromComponent, component.Name, StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.Rows);
            var data = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["component"] = component.Name,
                ["startedAt"] = component.StartedAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                ["endedAt"] = component.EndedAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                ["succeeded"] = component.Succeeded,
                ["failed"] = string.Equals(component.Name, report.FailedComponent, StringComparison.OrdinalIgnoreCase),
                ["rowsOut"] = rowsOut,
            };

            // Why the stage only when there is one: a component with none has no number, and "absent" is what the record says (FDW-2060).
            if (component.Stage > 0)
            {
                data["stage"] = component.Stage;
            }

            await Record(stage, ComponentEvent, $"Component '{component.Name}' finished", data, cancellationToken).ConfigureAwait(false);
        }

        await RecordRejections(stage, report, cancellationToken).ConfigureAwait(false);
        foreach (var count in report.Links)
        {
            await Record(stage, LinkEvent, $"{count.Link.FromComponent}.{count.Link.FromOutput} to {count.Link.ToComponent}.{count.Link.ToInput}", new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["from"] = count.Link.FromComponent,
                ["fromOutput"] = count.Link.FromOutput,
                ["to"] = count.Link.ToComponent,
                ["toInput"] = count.Link.ToInput,
                ["rows"] = count.Rows,
                ["batches"] = count.Batches,
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    // Why one event for each error output: it says how many rows left through the output and the reasons they gave, so a person sees what was
    // rejected and why. It holds no row: a row can hold a private value, and a pipeline that wants its rejects writes them to a destination.
    // A report that rejected nothing has nothing to record.
    private async Task RecordRejections(Guid stage, FlowRunReport report, CancellationToken cancellationToken)
    {
        if (report.Rejections is not { } rejections)
        {
            return;
        }

        foreach (var rejection in rejections)
        {
            await Record(stage, RejectedRowsEvent, $"Component '{rejection.Component}' rejected {rejection.Count} row(s) through '{rejection.Output}'", new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["component"] = rejection.Component,
                ["output"] = rejection.Output,
                ["count"] = rejection.Count,
                ["reasons"] = rejection.Reasons.Select(reason => new Dictionary<string, object?>(StringComparer.Ordinal) { ["reason"] = reason.Reason, ["count"] = reason.Count }).ToList(),
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task Record(Guid stage, string eventType, string message, IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken)
    {
        var recorded = await _tracker.RecordEvent(stage, eventType, message, data, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (recorded.IsFailure)
        {
            GraphLog.ReportNotWritten(_logger, message, recorded.CurrentMessage);
        }
    }
}
