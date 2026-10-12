using System;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Work item enqueued by endpoints and dequeued by the project orchestrator background service.
/// </summary>
/// <remarks>
/// Mirrors the shape of <c>PipelineExecutionRequest</c> in
/// <c>Fdw.Services.Etl.Abstractions</c>. Uses <c>required</c> properties to enforce
/// construction correctness without a parameter-heavy constructor.
/// </remarks>
public sealed class ProjectExecutionRequest
{
    /// <summary>
    /// Gets the execution tracking ID created by IExecutionTracker before enqueue.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the name of the project to execute.
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets the source that triggered this execution (e.g., "Api", "Scheduler").
    /// </summary>
    public required string TriggerSource { get; init; }
}
