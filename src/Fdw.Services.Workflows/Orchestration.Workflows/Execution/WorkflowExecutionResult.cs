using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

namespace Fdw.Orchestration.Workflows.Execution;

/// <summary>
/// Concrete implementation of workflow execution result.
/// </summary>
public sealed class WorkflowExecutionResult : IWorkflowExecutionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionResult"/> class.
    /// </summary>
    public WorkflowExecutionResult(
        string workflowExecutionId,
        string workflowId,
        IWorkflowExecutionStatus status,
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        IReadOnlyList<IWorkflowStepResult> stepResults,
        string? error = null,
        IReadOnlyDictionary<string, object>? errorDetails = null)
    {
        WorkflowExecutionId = workflowExecutionId;
        WorkflowId = workflowId;
        Status = status;
        StartTime = startTime;
        EndTime = endTime;
        Duration = endTime.HasValue ? endTime.Value - startTime : TimeSpan.Zero;
        StepResults = stepResults;
        Error = error;
        ErrorDetails = errorDetails;
        SuccessfulSteps = stepResults.Count(r => string.Equals(r.Status.Name, "Succeeded", StringComparison.Ordinal));
        FailedSteps = stepResults.Count(r => string.Equals(r.Status.Name, "Failed", StringComparison.Ordinal));
        SkippedSteps = stepResults.Count(r => string.Equals(r.Status.Name, "Cancelled", StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public string WorkflowExecutionId { get; }

    /// <inheritdoc/>
    public string WorkflowId { get; }

    /// <inheritdoc/>
    public IWorkflowExecutionStatus Status { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public DateTimeOffset? EndTime { get; }

    /// <inheritdoc/>
    public TimeSpan Duration { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IWorkflowStepResult> StepResults { get; }

    /// <inheritdoc/>
    public string? Error { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object>? ErrorDetails { get; }

    /// <inheritdoc/>
    public int SuccessfulSteps { get; }

    /// <inheritdoc/>
    public int FailedSteps { get; }

    /// <inheritdoc/>
    public int SkippedSteps { get; }

    /// <summary>
    /// Creates a success result from step results.
    /// </summary>
    public static WorkflowExecutionResult FromStepResults(
        string workflowExecutionId,
        string workflowId,
        DateTimeOffset startTime,
        IReadOnlyList<IWorkflowStepResult> stepResults)
    {
        var hasFailures = stepResults.Any(r => string.Equals(r.Status.Name, "Failed", StringComparison.Ordinal));
        var status = hasFailures
            ? WorkflowExecutionStatuses.ByName("Failed")
            : WorkflowExecutionStatuses.ByName("Succeeded");

        return new WorkflowExecutionResult(
            workflowExecutionId,
            workflowId,
            status,
            startTime,
            DateTimeOffset.UtcNow,
            stepResults,
            hasFailures ? "One or more steps failed" : null);
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static WorkflowExecutionResult Failure(
        string workflowExecutionId,
        string workflowId,
        DateTimeOffset startTime,
        IReadOnlyList<IWorkflowStepResult> stepResults,
        string error,
        IReadOnlyDictionary<string, object>? errorDetails = null)
    {
        return new WorkflowExecutionResult(
            workflowExecutionId,
            workflowId,
            WorkflowExecutionStatuses.ByName("Failed"),
            startTime,
            DateTimeOffset.UtcNow,
            stepResults,
            error,
            errorDetails);
    }
}
