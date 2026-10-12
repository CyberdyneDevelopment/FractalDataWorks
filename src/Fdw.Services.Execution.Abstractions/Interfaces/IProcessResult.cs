using System;
using System.Collections.Generic;
using Fdw.Services.Execution.Abstractions.OptionTypes;

namespace Fdw.Services.Execution.Abstractions;

/// <summary>
/// Result of a process operation execution.
/// </summary>
public interface IProcessResult
{
    /// <summary>
    /// Unique identifier for the process that generated this result.
    /// </summary>
    string ProcessId { get; }

    /// <summary>
    /// Name of the operation that was executed.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Final state of the process after operation execution.
    /// </summary>
    IProcessState FinalState { get; }

    /// <summary>
    /// Result data from the operation (if any).
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Detailed error information.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Performance metrics for the operation.
    /// </summary>
    IProcessMetrics Metrics { get; }

    /// <summary>
    /// Additional metadata from the operation.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// When the operation started.
    /// </summary>
    DateTime StartedAt { get; }

    /// <summary>
    /// When the operation completed.
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Total duration of the operation.
    /// </summary>
    TimeSpan? Duration { get; }
}