using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.SignalR;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Logging;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Default implementation of <see cref="IPipelineStatusBroadcaster"/> using SignalR.
/// Task and edge updates are coalesced at <c>BroadcastHz</c> (default 5 Hz) to prevent
/// flooding connected clients during high-throughput executions.
/// </summary>
public sealed class PipelineStatusBroadcaster
    : SignalRBroadcaster<PipelineStatusHub, IPipelineStatusHubClient>, IPipelineStatusBroadcaster
{
    private readonly ILogger<PipelineStatusBroadcaster> _logger;
    private readonly int _broadcastHz;
    private readonly long _sampleBufferMaxBytes;

    // Why: Per-(executionId, key) last-broadcast timestamps enable coalescing without a global
    // lock. ConcurrentDictionary is safe for concurrent reads and writes across the pipeline
    // batch loop and the SignalR timer.
    private readonly ConcurrentDictionary<string, long> _lastBroadcastTick = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineStatusBroadcaster"/>.
    /// </summary>
    public PipelineStatusBroadcaster(
        IHubContext<PipelineStatusHub, IPipelineStatusHubClient> hubContext,
        ILogger<PipelineStatusBroadcaster>? logger = null,
        IOptions<PipelineStatusBroadcasterOptions>? options = null)
        : base(hubContext, logger ?? NullLogger<PipelineStatusBroadcaster>.Instance)
    {
        _logger = logger ?? NullLogger<PipelineStatusBroadcaster>.Instance;
        var resolved = options?.Value ?? new PipelineStatusBroadcasterOptions();
        _broadcastHz = resolved.BroadcastHz > 0 ? resolved.BroadcastHz : 5;
        _sampleBufferMaxBytes = resolved.SampleBufferMaxBytes > 0 ? resolved.SampleBufferMaxBytes : 10_000_000;
        // Why: Log the resolved configuration at startup so administrators can verify their
        // appsettings values were picked up correctly (plan decision 0c5).
        SignalRBroadcasterLog.BroadcasterConfigured(_logger, _broadcastHz, _sampleBufferMaxBytes);
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

    // Why: the org-scoped firehose replaces the removed global "pipeline-updates" firehose. When the
    // pipeline has an owning org, its lifecycle events also fan out to org:{orgId}:pipeline-updates so
    // an "all my pipelines" view scoped to that org receives them; a null org targets no firehose
    // (there is no global cross-org group) — the pipeline/execution groups still deliver to explicit
    // subscribers. No fallback to a global group: a missing org means no org firehose, logged.
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

        // Why: terminal statuses (Complete, Failed) always bypass coalescing so clients see
        // the final state even if they missed the last coalesced update.
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
        || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);
}

