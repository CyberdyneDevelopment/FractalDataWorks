using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions.Commands;

/// <summary>
/// Interface for individual command results within a batch execution.
/// Provides detailed information about a single command's execution outcome.
/// </summary>
/// <remarks>
/// Command results provide comprehensive information about individual command executions
/// within a batch, enabling fine-grained result processing and error handling.
/// </remarks>
public interface IGenericCommandResult
{
    /// <summary>
    /// Gets the identifier of the command this result belongs to.
    /// </summary>
    /// <value>The command identifier from the original IDataCommand.</value>
    string CommandId { get; }

    /// <summary>
    /// Gets the type of the command this result belongs to.
    /// </summary>
    /// <value>The command type from the original IDataCommand.</value>
    string CommandType { get; }

    /// <summary>
    /// Gets the position of this command in the original batch.
    /// </summary>
    /// <value>The zero-based index of the command in the batch.</value>
    /// <remarks>
    /// The batch position helps correlate results with the original command order,
    /// especially when batch execution may reorder commands for optimization.
    /// </remarks>
    int BatchPosition { get; }

    /// <summary>
    /// Gets a value indicating whether the command executed successfully.
    /// </summary>
    /// <value><c>true</c> if the command succeeded; otherwise, <c>false</c>.</value>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the result data returned by the command, if any.
    /// </summary>
    /// <value>The command result data, or null if the command returned no data or failed.</value>
    /// <remarks>
    /// Result data contains the output of successful command execution. The type and
    /// structure of the data depend on the specific command and data provider used.
    /// </remarks>
    object? ResultData { get; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    /// <value>The error message describing the failure, or null if the command succeeded.</value>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets additional error details if the command failed.
    /// </summary>
    /// <value>A collection of detailed error information, or empty if the command succeeded.</value>
    /// <remarks>
    /// Error details may include stack traces, provider-specific error codes,
    /// constraint violation details, or other diagnostic information useful for troubleshooting.
    /// </remarks>
    IReadOnlyList<string> ErrorDetails { get; }

    /// <summary>
    /// Gets the exception that caused the command failure, if any.
    /// </summary>
    /// <value>The exception that occurred during command execution, or null if no exception occurred.</value>
    Exception? Exception { get; }

    /// <summary>
    /// Gets the time taken to execute this command.
    /// </summary>
    /// <value>The duration of command execution, or null if timing information is not available.</value>
    /// <remarks>
    /// Execution time includes data provider processing time but may not include
    /// time spent waiting in queues or for batch coordination.
    /// </remarks>
    TimeSpan? ExecutionTime { get; }

    /// <summary>
    /// Gets the timestamp when this command started executing.
    /// </summary>
    /// <value>The UTC timestamp when command execution began, or null if not available.</value>
    DateTimeOffset? StartedAt { get; }

    /// <summary>
    /// Gets the timestamp when this command completed executing.
    /// </summary>
    /// <value>The UTC timestamp when command execution finished, or null if not available.</value>
    DateTimeOffset? CompletedAt { get; }

    /// <summary>
    /// Gets additional metadata about this command's execution.
    /// </summary>
    /// <value>A dictionary of metadata properties related to command execution.</value>
    /// <remarks>
    /// Command metadata may include provider-specific information, optimization details,
    /// performance metrics, or other data relevant to understanding the command execution.
    /// </remarks>
    IReadOnlyDictionary<string, object> CommandMetadata { get; }
}
