using System;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Result of a single step execution.
/// </summary>
public interface IOrchestrationStepResult
{
    /// <summary>
    /// Gets the step ID.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the step name.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Gets the execution status of this step.
    /// </summary>
    IExecutionStatus Status { get; }

    /// <summary>
    /// Gets the time when step execution started.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the time when step execution ended.
    /// </summary>
    DateTimeOffset? EndTime { get; }

    /// <summary>
    /// Gets the step execution duration.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets the error message if the step failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception if the step failed due to an exception.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    int RetryAttempts { get; }

    /// <summary>
    /// Gets the output of this step, if any.
    /// </summary>
    object? Output { get; }

    /// <summary>
    /// Gets the number of records processed by this step.
    /// </summary>
    long RecordsProcessed { get; }

    /// <summary>
    /// Gets a value indicating whether this result was retrieved from cache.
    /// </summary>
    bool WasCached { get; }
}

/// <summary>
/// Generic step result interface with typed output.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IOrchestrationStepResult<TOutput> : IOrchestrationStepResult
{
    /// <summary>
    /// Gets the typed output of this step.
    /// </summary>
    new TOutput? Output { get; }
}