using System;
using System.Collections.Generic;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Result of an orchestration execution.
/// </summary>
public interface IOrchestrationResult
{
    /// <summary>
    /// Gets the execution ID.
    /// </summary>
    string ExecutionId { get; }

    /// <summary>
    /// Gets the orchestration ID that was executed.
    /// </summary>
    string OrchestrationId { get; }

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    IExecutionStatus Status { get; }

    /// <summary>
    /// Gets the time when execution started.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the time when execution ended.
    /// </summary>
    DateTimeOffset? EndTime { get; }

    /// <summary>
    /// Gets the total execution duration.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception if execution failed due to an exception.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets the results of individual steps.
    /// </summary>
    IReadOnlyList<IOrchestrationStepResult> StepResults { get; }

    /// <summary>
    /// Gets the final output of the orchestration, if any.
    /// </summary>
    object? Output { get; }

    /// <summary>
    /// Gets metrics collected during execution.
    /// </summary>
    IOrchestrationMetrics? Metrics { get; }
}

/// <summary>
/// Generic result interface with typed output.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IOrchestrationResult<TOutput> : IOrchestrationResult
{
    /// <summary>
    /// Gets the typed output of the orchestration.
    /// </summary>
    new TOutput? Output { get; }
}