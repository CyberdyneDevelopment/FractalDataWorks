using System;
using System.Collections.Generic;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Result of a workflow step execution.
/// </summary>
public interface IWorkflowStepResult
{
    /// <summary>
    /// Gets the step ID.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the step execution status.
    /// </summary>
    IExecutionStatus Status { get; }

    /// <summary>
    /// Gets when the step started.
    /// </summary>
    DateTimeOffset? StartTime { get; }

    /// <summary>
    /// Gets when the step ended.
    /// </summary>
    DateTimeOffset? EndTime { get; }

    /// <summary>
    /// Gets the step duration.
    /// </summary>
    TimeSpan? Duration { get; }

    /// <summary>
    /// Gets the pipeline execution ID (if step was a pipeline).
    /// </summary>
    string? PipelineExecutionId { get; }

    /// <summary>
    /// Gets any error that occurred.
    /// </summary>
    string? Error { get; }

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// Gets step output data.
    /// </summary>
    IReadOnlyDictionary<string, object?>? OutputData { get; }
}
