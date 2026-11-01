using System;
using System.Collections.Generic;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Represents the persisted state of an orchestration execution.
/// </summary>
public sealed class ExecutionState
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the orchestration ID.
    /// </summary>
    public string OrchestrationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the orchestration version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current step ID being executed.
    /// </summary>
    public string? CurrentStepId { get; set; }

    /// <summary>
    /// Gets or sets the IDs of completed steps.
    /// </summary>
    public IList<string> CompletedStepIds { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the shared state dictionary.
    /// </summary>
    public IDictionary<string, object?> SharedState { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the input parameters.
    /// </summary>
    public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the step results.
    /// </summary>
    public IList<IOrchestrationStepResult> StepResults { get; set; } = new List<IOrchestrationStepResult>();

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public System.DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets when the state was last updated.
    /// </summary>
    public System.DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the execution status name.
    /// </summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the execution is paused.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the reason for pausing, if paused.
    /// </summary>
    public string? PauseReason { get; set; }
}