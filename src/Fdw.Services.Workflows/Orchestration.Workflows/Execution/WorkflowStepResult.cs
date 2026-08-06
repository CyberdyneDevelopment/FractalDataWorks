using System;
using System.Collections.Generic;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using Fdw.Orchestration.Workflows.Abstractions;

namespace Fdw.Orchestration.Workflows.Execution;

/// <summary>
/// Concrete implementation of workflow step result.
/// </summary>
public sealed class WorkflowStepResult : IWorkflowStepResult
{
    private WorkflowStepResult(
        string stepId,
        IExecutionStatus status,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        string? pipelineExecutionId,
        string? error,
        int retryCount,
        IReadOnlyDictionary<string, object?>? outputData)
    {
        StepId = stepId;
        Status = status;
        StartTime = startTime;
        EndTime = endTime;
        Duration = startTime.HasValue && endTime.HasValue ? endTime.Value - startTime.Value : null;
        PipelineExecutionId = pipelineExecutionId;
        Error = error;
        RetryCount = retryCount;
        OutputData = outputData;
    }

    /// <inheritdoc/>
    public string StepId { get; }

    /// <inheritdoc/>
    public IExecutionStatus Status { get; }

    /// <inheritdoc/>
    public DateTimeOffset? StartTime { get; }

    /// <inheritdoc/>
    public DateTimeOffset? EndTime { get; }

    /// <inheritdoc/>
    public TimeSpan? Duration { get; }

    /// <inheritdoc/>
    public string? PipelineExecutionId { get; }

    /// <inheritdoc/>
    public string? Error { get; }

    /// <inheritdoc/>
    public int RetryCount { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?>? OutputData { get; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static WorkflowStepResult Success(
        string stepId,
        DateTimeOffset startTime,
        string? pipelineExecutionId = null,
        IReadOnlyDictionary<string, object?>? outputData = null)
    {
        return new WorkflowStepResult(
            stepId,
            ExecutionStatuses.ByName("Succeeded"),
            startTime,
            DateTimeOffset.UtcNow,
            pipelineExecutionId,
            null,
            0,
            outputData);
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static WorkflowStepResult Failure(
        string stepId,
        DateTimeOffset startTime,
        string error,
        int retryCount = 0)
    {
        return new WorkflowStepResult(
            stepId,
            ExecutionStatuses.ByName("Failed"),
            startTime,
            DateTimeOffset.UtcNow,
            null,
            error,
            retryCount,
            null);
    }

    /// <summary>
    /// Creates a skipped result.
    /// </summary>
    public static WorkflowStepResult Skipped(
        string stepId,
        string reason)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowStepResult(
            stepId,
            ExecutionStatuses.ByName("Cancelled"),
            now,
            now,
            null,
            reason,
            0,
            null);
    }

    /// <summary>
    /// Creates a running result.
    /// </summary>
    public static WorkflowStepResult Running(
        string stepId,
        DateTimeOffset startTime)
    {
        return new WorkflowStepResult(
            stepId,
            ExecutionStatuses.ByName("Running"),
            startTime,
            null,
            null,
            null,
            0,
            null);
    }
}
