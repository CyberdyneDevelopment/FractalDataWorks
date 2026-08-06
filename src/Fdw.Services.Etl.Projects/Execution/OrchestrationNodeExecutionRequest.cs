using System;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Work item enqueued by endpoints and dequeued by the orchestrator background service.
/// Uses the root node's logical Id as the execution target rather than a name string,
/// enabling arbitrary tree depth without name collision concerns.
/// </summary>
public sealed class OrchestrationNodeExecutionRequest
{
    /// <summary>
    /// Gets the execution tracking ID created by IExecutionTracker before enqueue.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the logical Id of the root OrchestrationNode to execute.
    /// </summary>
    public required Guid RootNodeId { get; init; }

    /// <summary>
    /// Gets the source that triggered this execution (e.g., "Api", "Scheduler").
    /// </summary>
    public required string TriggerSource { get; init; }

    /// <summary>
    /// Gets the tenant this execution belongs to, if known (mirrors
    /// <c>OrchestrationNodeConfiguration.TenantId</c> on the root node). Null when no tenant scope
    /// applies. See <c>PipelineExecutionRequest.TenantId</c> remarks for why this must ride on the
    /// request rather than be resolved from ambient context after dequeue.
    /// </summary>
    public Guid? TenantId { get; init; }
}
