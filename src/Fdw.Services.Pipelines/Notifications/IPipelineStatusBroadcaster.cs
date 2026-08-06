using System;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Hubs;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Service to broadcast pipeline status updates to connected SignalR clients.
/// </summary>
public interface IPipelineStatusBroadcaster
{
    /// <summary>
    /// Broadcasts a status change for a pipeline execution.
    /// </summary>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="orgId">
    /// The owning organization of the pipeline, used to scope the broadcast to that org's firehose
    /// (<c>org:{orgId}:pipeline-updates</c>). <see langword="null"/> (the default) means no org
    /// firehose is targeted — the pipeline/execution groups still receive it, and there is no global
    /// cross-org firehose. The authoritative execution path (the background executor) always passes the
    /// pipeline's owning org; direct callers that do not resolve it leave it null.
    /// </param>
    Task BroadcastStatusChange(string pipelineName, Guid executionId, string status, string? message = null, Guid? orgId = null);

    /// <summary>
    /// Broadcasts progress update for a pipeline execution. The trailing <c>orgId</c> scopes the
    /// broadcast to the owning org's firehose (see <see cref="BroadcastStatusChange"/>).
    /// </summary>
    Task BroadcastProgress(
        string pipelineName,
        Guid executionId,
        int recordsExtracted,
        int recordsTransformed,
        int recordsLoaded,
        int recordsFailed,
        int progressPercentage,
        Guid? orgId = null);

    /// <summary>
    /// Broadcasts execution completion for a pipeline.
    /// </summary>
    /// <param name="completion">The completion details.</param>
    /// <param name="orgId">The owning org for firehose scoping; see <see cref="BroadcastStatusChange"/>.</param>
    Task BroadcastCompletion(PipelineExecutionComplete completion, Guid? orgId = null);

    /// <summary>
    /// Broadcasts per-task status and counter update to the <c>execution:{executionId}</c> group.
    /// Coalesced at <c>BroadcastHz</c> frequency; the final (terminal) broadcast is always sent.
    /// </summary>
    Task BroadcastTaskStatus(
        Guid executionId,
        Guid taskId,
        string status,
        long recordsIn,
        long recordsOut,
        long recordsDiscarded,
        long recordsHeld,
        bool sampleBufferAtCapacity);

    /// <summary>
    /// Broadcasts the current record-flow count for an edge.
    /// Coalesced at <c>BroadcastHz</c> frequency.
    /// </summary>
    Task BroadcastEdgeFlow(
        Guid executionId,
        Guid sourceTaskId,
        Guid targetTaskId,
        long recordsFlowed);

    /// <summary>
    /// Broadcasts that a test execution has been paused.
    /// </summary>
    Task BroadcastExecutionPaused(Guid executionId);

    /// <summary>
    /// Broadcasts that a test execution has been resumed.
    /// </summary>
    Task BroadcastExecutionResumed(Guid executionId);
}
