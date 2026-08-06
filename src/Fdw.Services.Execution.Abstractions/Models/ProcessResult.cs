using System;
using System.Collections.Generic;
using Fdw.Services.Execution.Abstractions;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Execution.Abstractions.Models;

/// <summary>
/// Default implementation of process result.
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessResult : IProcessResult
{
    /// <summary>
    /// Unique identifier for the process that generated this result.
    /// </summary>
    public string ProcessId { get; init; } = string.Empty;

    /// <summary>
    /// Name of the operation that was executed.
    /// </summary>
    public string OperationName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Final state of the process after operation execution.
    /// </summary>
    public required IProcessState FinalState { get; init; }

    /// <summary>
    /// Result data from the operation (if any).
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Detailed error information.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Performance metrics for the operation.
    /// </summary>
    public IProcessMetrics Metrics { get; init; } = ProcessMetrics.Empty;

    /// <summary>
    /// Additional metadata from the operation.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// When the operation started.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the operation completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Total duration of the operation.
    /// </summary>
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);

    /// <summary>
    /// Get a successful result.
    /// </summary>
    /// <param name="processId">Unique identifier for the process.</param>
    /// <param name="operationName">Name of the executed operation.</param>
    /// <param name="finalState">Final state of the process.</param>
    /// <param name="data">Optional result data.</param>
    /// <param name="metrics">Optional performance metrics.</param>
    /// <returns>A successful ProcessResult instance.</returns>
    public static ProcessResult Success(
        string processId,
        string operationName,
        IProcessState finalState,
        object? data = null,
        IProcessMetrics? metrics = null)
    {
        return new ProcessResult
        {
            ProcessId = processId,
            OperationName = operationName,
            IsSuccess = true,
            FinalState = finalState,
            Data = data,
            Metrics = metrics ?? ProcessMetrics.Empty,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get a failed result.
    /// </summary>
    /// <param name="processId">Unique identifier for the process.</param>
    /// <param name="operationName">Name of the executed operation.</param>
    /// <param name="finalState">Final state of the process.</param>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>A failed ProcessResult instance.</returns>
    public static ProcessResult Failure(
        string processId,
        string operationName,
        IProcessState finalState,
        string errorMessage,
        Exception? exception = null)
    {
        return new ProcessResult
        {
            ProcessId = processId,
            OperationName = operationName,
            IsSuccess = false,
            FinalState = finalState,
            ErrorMessage = errorMessage,
            Exception = exception,
            CompletedAt = DateTime.UtcNow
        };
    }
}