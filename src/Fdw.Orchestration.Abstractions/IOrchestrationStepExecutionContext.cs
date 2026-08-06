using System;
using System.Collections.Generic;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Provides context for step execution within an orchestration.
/// </summary>
/// <remarks>
/// This context provides step-specific execution state, input/output data,
/// and access to the parent orchestration context.
/// </remarks>
public interface IOrchestrationStepExecutionContext
{
    /// <summary>
    /// Gets the unique identifier for this step execution.
    /// </summary>
    string StepExecutionId { get; }

    /// <summary>
    /// Gets the step ID being executed.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the step being executed.
    /// </summary>
    IOrchestrationStep Step { get; }

    /// <summary>
    /// Gets the parent orchestration execution context.
    /// </summary>
    IOrchestrationContext OrchestrationContext { get; }

    /// <summary>
    /// Gets the execution start time for this step.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the current attempt number (1-based, increments on retries).
    /// </summary>
    int AttemptNumber { get; }

    /// <summary>
    /// Gets the step-specific state dictionary.
    /// </summary>
    IDictionary<string, object?> StepState { get; }

    /// <summary>
    /// Gets input data from previous steps or orchestration parameters.
    /// </summary>
    IReadOnlyDictionary<string, object?> InputData { get; }

    /// <summary>
    /// Gets or sets output data for subsequent steps.
    /// </summary>
    IDictionary<string, object?> OutputData { get; }
}
