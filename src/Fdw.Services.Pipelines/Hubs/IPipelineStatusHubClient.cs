using System;
using System.Threading.Tasks;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Client interface for Pipeline Status Hub notifications.
/// </summary>
public interface IPipelineStatusHubClient
{
    /// <summary>
    /// Called when a pipeline execution status changes.
    /// </summary>
    Task OnStatusChanged(PipelineStatusUpdate update);

    /// <summary>
    /// Called when execution progress is updated.
    /// </summary>
    Task OnProgressUpdated(PipelineProgressUpdate update);

    /// <summary>
    /// Called when a pipeline execution completes.
    /// </summary>
    Task OnExecutionCompleted(PipelineExecutionComplete complete);

    /// <summary>
    /// Called when a task node's status or counters change during a test or live execution.
    /// Broadcast at most once per <c>BroadcastHz</c> interval; final broadcast always sent.
    /// </summary>
    Task OnTaskStatusChanged(PipelineTaskStatusUpdate update);

    /// <summary>
    /// Called when records flow across an edge during execution.
    /// Coalesced at <c>BroadcastHz</c> interval.
    /// </summary>
    Task OnEdgeFlow(PipelineEdgeFlowUpdate update);

    /// <summary>
    /// Called when a test execution is paused.
    /// </summary>
    Task OnExecutionPaused(Guid executionId);

    /// <summary>
    /// Called when a test execution is resumed.
    /// </summary>
    Task OnExecutionResumed(Guid executionId);
}
