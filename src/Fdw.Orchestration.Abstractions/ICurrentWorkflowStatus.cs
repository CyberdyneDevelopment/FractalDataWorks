using System;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Current status of a workflow execution.
/// </summary>
public interface ICurrentWorkflowStatus
{
    /// <summary>
    /// Gets the workflow execution ID.
    /// </summary>
    string WorkflowExecutionId { get; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    IWorkflowExecutionStatus Status { get; }

    /// <summary>
    /// Gets the current step being executed.
    /// </summary>
    string? CurrentStepId { get; }

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    double ProgressPercentage { get; }

    /// <summary>
    /// Gets the number of completed steps.
    /// </summary>
    int CompletedSteps { get; }

    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    int TotalSteps { get; }

    /// <summary>
    /// Gets when the workflow started.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the elapsed time.
    /// </summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    TimeSpan? EstimatedTimeRemaining { get; }
}