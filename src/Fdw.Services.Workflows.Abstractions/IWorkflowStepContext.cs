using System;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Context for a workflow step execution.
/// </summary>
public interface IWorkflowStepContext
{
    /// <summary>
    /// Gets the step identifier.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the step name.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Gets the step start time.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the retry count for this step.
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// Gets whether this is a compensation execution.
    /// </summary>
    bool IsCompensation { get; }
}