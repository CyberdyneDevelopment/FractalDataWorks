using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Execution.Abstractions.OptionTypes;

namespace Fdw.Services.Execution.Abstractions;

/// <summary>
/// Core interface for any process that can be executed in the Fdw system.
/// Represents a unit of work that can be run directly or scheduled.
/// </summary>
public interface IProcess
{
    /// <summary>
    /// Unique identifier for this process instance.
    /// </summary>
    string ProcessId { get; }

    /// <summary>
    /// Name of the process type (e.g., "ETL", "HealthCheck").
    /// </summary>
    string ProcessTypeName { get; }

    /// <summary>
    /// Current state of the process.
    /// </summary>
    IProcessState State { get; }

    /// <summary>
    /// Configuration for this process instance.
    /// </summary>
    object Configuration { get; }

    /// <summary>
    /// Execute an operation on this process.
    /// </summary>
    /// <param name="operationName">Name of the operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<IProcessResult> Execute(string operationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this process supports the specified operation.
    /// </summary>
    /// <param name="operationName">Name of the operation to check.</param>
    /// <returns>True if the operation is supported.</returns>
    bool SupportsOperation(string operationName);

    /// <summary>
    /// Get all operations supported by this process.
    /// </summary>
    /// <returns>Collection of supported operation names.</returns>
    string[] GetSupportedOperations();
}