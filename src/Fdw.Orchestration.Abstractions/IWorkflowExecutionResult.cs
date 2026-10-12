using System;
using System.Collections.Generic;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Workflow execution result.
/// </summary>
public interface IWorkflowExecutionResult
{
    /// <summary>
    /// Gets the workflow execution ID.
    /// </summary>
    string WorkflowExecutionId { get; }

    /// <summary>
    /// Gets the workflow ID.
    /// </summary>
    string WorkflowId { get; }

    /// <summary>
    /// Gets the execution status value.
    /// </summary>
    IWorkflowExecutionStatus Status { get; }

    /// <summary>
    /// Gets when the workflow started.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets when the workflow ended.
    /// </summary>
    DateTimeOffset? EndTime { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets the step results.
    /// </summary>
    IReadOnlyList<IWorkflowStepResult> StepResults { get; }

    /// <summary>
    /// Gets any error that occurred.
    /// </summary>
    string? Error { get; }

    /// <summary>
    /// Gets the error details.
    /// </summary>
    IReadOnlyDictionary<string, object>? ErrorDetails { get; }

    /// <summary>
    /// Gets the number of successful steps.
    /// </summary>
    int SuccessfulSteps { get; }

    /// <summary>
    /// Gets the number of failed steps.
    /// </summary>
    int FailedSteps { get; }

    /// <summary>
    /// Gets the number of skipped steps.
    /// </summary>
    int SkippedSteps { get; }
}