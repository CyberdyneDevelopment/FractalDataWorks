using System;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

namespace Fdw.Orchestration.Workflows.Execution;

/// <summary>
/// Concrete implementation of current workflow status.
/// </summary>
public sealed class CurrentWorkflowStatus : ICurrentWorkflowStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentWorkflowStatus"/> class.
    /// </summary>
    public CurrentWorkflowStatus(
        string workflowExecutionId,
        IWorkflowExecutionStatus status,
        string? currentStepId,
        int completedSteps,
        int totalSteps,
        DateTimeOffset startTime)
    {
        WorkflowExecutionId = workflowExecutionId;
        Status = status;
        CurrentStepId = currentStepId;
        CompletedSteps = completedSteps;
        TotalSteps = totalSteps;
        StartTime = startTime;
        ElapsedTime = DateTimeOffset.UtcNow - startTime;
        ProgressPercentage = totalSteps > 0 ? (double)completedSteps / totalSteps * 100 : 0;

        // Estimate remaining time based on average step time
        if (completedSteps > 0 && completedSteps < totalSteps)
        {
            var avgStepTime = ElapsedTime / completedSteps;
            var remainingSteps = totalSteps - completedSteps;
            EstimatedTimeRemaining = TimeSpan.FromTicks(avgStepTime.Ticks * remainingSteps);
        }
    }

    /// <inheritdoc/>
    public string WorkflowExecutionId { get; }

    /// <inheritdoc/>
    public IWorkflowExecutionStatus Status { get; }

    /// <inheritdoc/>
    public string? CurrentStepId { get; }

    /// <inheritdoc/>
    public double ProgressPercentage { get; }

    /// <inheritdoc/>
    public int CompletedSteps { get; }

    /// <inheritdoc/>
    public int TotalSteps { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public TimeSpan ElapsedTime { get; }

    /// <inheritdoc/>
    public TimeSpan? EstimatedTimeRemaining { get; }
}
