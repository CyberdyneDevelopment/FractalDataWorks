using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.ServiceTypes;
using Fdw.SignalR;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Logging;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Default implementation of <see cref="IPipelineStatusBroadcaster"/> using SignalR.
/// Task and edge updates are coalesced at the pipeline hub's declared <c>BroadcastHz</c> (FDW-3956) to prevent
/// flooding connected clients during high-throughput executions.
/// </summary>
public sealed class PipelineStatusBroadcaster
    : SignalRBroadcaster<PipelineStatusHub, IPipelineStatusHubClient>, IPipelineStatusBroadcaster
{
    private readonly ILogger<PipelineStatusBroadcaster> _logger;
    private readonly int _broadcastHz;

    private readonly ConcurrentDictionary<string, long> _lastBroadcastTick = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineStatusBroadcaster"/>.
    /// </summary>
    /// <remarks>
    /// The coalescing rate is the pipeline hub's declared <see cref="PipelineHubOption.BroadcastHz"/>, or a host's override of it (FDW-3956);
    /// nothing in this class assumes one. A rate that is missing or not positive stops the broadcaster from being built, naming the key.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The hub declares no positive rate and no host overrides it.</exception>
    public PipelineStatusBroadcaster(
        IHubContext<PipelineStatusHub, IPipelineStatusHubClient> hubContext,
        ILogger<PipelineStatusBroadcaster>? logger = null)
        : base(hubContext, logger ?? NullLogger<PipelineStatusBroadcaster>.Instance)
    {
        _logger = logger ?? NullLogger<PipelineStatusBroadcaster>.Instance;
        if (!ServiceTypeDefaults.TryResolve(new PipelineHubOption(), PipelineHubOption.BroadcastHz, out var declared) || declared is not int hz || hz <= 0)
        {
            throw new InvalidOperationException(
                SignalRBroadcasterLog.BroadcastRateNotDeclared(_logger, PipelineHubOption.BroadcastHz, declared?.ToString() ?? string.Empty).Message);
        }

        _broadcastHz = hz;
        SignalRBroadcasterLog.BroadcasterConfigured(_logger, _broadcastHz);
    }

    /// <inheritdoc/>
    public Task BroadcastStatusChange(string pipelineName, Guid executionId, string status, string? message = null, Guid? orgId = null)
    {
        var update = new PipelineStatusUpdate
        {
            PipelineName = pipelineName,
            ExecutionId = executionId,
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return BroadcastToGroups(
            update,
            (client, evt) => client.OnStatusChanged(evt),
            FirehoseGroups(orgId, pipelineName, executionId));
    }

    /// <inheritdoc/>
    public Task BroadcastProgress(
        string pipelineName,
        Guid executionId,
        int recordsExtracted,
        int recordsTransformed,
        int recordsLoaded,
        int recordsFailed,
        int progressPercentage,
        Guid? orgId = null)
    {
        var update = new PipelineProgressUpdate
        {
            PipelineName = pipelineName,
            ExecutionId = executionId,
            RecordsExtracted = recordsExtracted,
            RecordsTransformed = recordsTransformed,
            RecordsLoaded = recordsLoaded,
            RecordsFailed = recordsFailed,
            ProgressPercentage = progressPercentage,
            Timestamp = DateTime.UtcNow
        };

        return BroadcastToGroups(
            update,
            (client, evt) => client.OnProgressUpdated(evt),
            FirehoseGroups(orgId, pipelineName, executionId));
    }

    /// <inheritdoc/>
    public Task BroadcastCompletion(PipelineExecutionComplete completion, Guid? orgId = null)
    {
        return BroadcastToGroups(
            completion,
            (client, evt) => client.OnExecutionCompleted(evt),
            FirehoseGroups(orgId, completion.PipelineName, completion.ExecutionId));
    }

    private string[] FirehoseGroups(Guid? orgId, string pipelineName, Guid executionId)
    {
        if (orgId.HasValue)
        {
            SignalRBroadcasterLog.OrgFirehoseTargeted(_logger, orgId.Value, executionId);
            return new[]
            {
                $"org:{orgId.Value}:pipeline-updates",
                $"pipeline:{pipelineName}",
                $"execution:{executionId}"
            };
        }

        SignalRBroadcasterLog.NoOrgFirehose(_logger, executionId);
        return new[] { $"pipeline:{pipelineName}", $"execution:{executionId}" };
    }

    /// <inheritdoc/>
    public Task BroadcastComponents(PipelineComponentsUpdate update, Guid? orgId = null)
        => BroadcastToGroups(
            update,
            (client, evt) => client.OnComponentsChanged(evt),
            FirehoseGroups(orgId, update.PipelineName, update.ExecutionId));

    /// <inheritdoc/>
    public Task BroadcastTaskStatus(
        Guid executionId,
        Guid taskId,
        string status,
        long recordsIn,
        long recordsOut,
        long recordsDiscarded,
        long recordsHeld,
        bool sampleBufferAtCapacity)
    {
        var coalesceKey = $"task:{executionId:N}:{taskId:N}";

        var isTerminal = IsTerminalStatus(status);
        if (!isTerminal && ShouldCoalesce(coalesceKey))
        {
            SignalRBroadcasterLog.TaskStatusBroadcastCoalesced(_logger, executionId, taskId);
            return Task.CompletedTask;
        }

        if (isTerminal)
        {
            SignalRBroadcasterLog.TaskStatusTerminalBroadcast(_logger, executionId, taskId);
        }

        RecordBroadcast(coalesceKey);

        var update = new PipelineTaskStatusUpdate
        {
            ExecutionId = executionId,
            TaskId = taskId,
            Status = status,
            RecordsIn = recordsIn,
            RecordsOut = recordsOut,
            RecordsDiscarded = recordsDiscarded,
            RecordsHeld = recordsHeld,
            SampleBufferAtCapacity = sampleBufferAtCapacity,
            Timestamp = DateTime.UtcNow
        };

        return BroadcastToGroups(
            update,
            (client, evt) => client.OnTaskStatusChanged(evt),
            $"execution:{executionId}");
    }

    /// <inheritdoc/>
    public Task BroadcastEdgeFlow(
        Guid executionId,
        Guid sourceTaskId,
        Guid targetTaskId,
        long recordsFlowed)
    {
        var coalesceKey = $"edge:{executionId:N}:{sourceTaskId:N}:{targetTaskId:N}";
        if (ShouldCoalesce(coalesceKey))
        {
            SignalRBroadcasterLog.EdgeFlowBroadcastCoalesced(_logger, executionId, sourceTaskId, targetTaskId);
            return Task.CompletedTask;
        }

        RecordBroadcast(coalesceKey);

        var update = new PipelineEdgeFlowUpdate
        {
            ExecutionId = executionId,
            SourceTaskId = sourceTaskId,
            TargetTaskId = targetTaskId,
            RecordsFlowed = recordsFlowed,
            Timestamp = DateTime.UtcNow
        };

        return BroadcastToGroups(
            update,
            (client, evt) => client.OnEdgeFlow(evt),
            $"execution:{executionId}");
    }

    /// <inheritdoc/>
    public Task BroadcastExecutionPaused(Guid executionId)
    {
        return BroadcastToGroups(
            executionId,
            (client, id) => client.OnExecutionPaused(id),
            $"execution:{executionId}");
    }

    /// <inheritdoc/>
    public Task BroadcastExecutionResumed(Guid executionId)
    {
        return BroadcastToGroups(
            executionId,
            (client, id) => client.OnExecutionResumed(id),
            $"execution:{executionId}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coalescing helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the broadcast for <paramref name="key"/> should be skipped because the
    /// minimum inter-broadcast interval has not elapsed.
    /// </summary>
    private bool ShouldCoalesce(string key)
    {
        if (_broadcastHz <= 0) return false;
        var intervalTicks = TimeSpan.FromMilliseconds(1000.0 / _broadcastHz).Ticks;
        var now = DateTime.UtcNow.Ticks;
        if (!_lastBroadcastTick.TryGetValue(key, out var last)) return false;
        return (now - last) < intervalTicks;
    }

    private void RecordBroadcast(string key)
    {
        _lastBroadcastTick[key] = DateTime.UtcNow.Ticks;
    }

    private static bool IsTerminalStatus(string status)
        => string.Equals(status, "Complete", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Interrupted", StringComparison.OrdinalIgnoreCase);
}

