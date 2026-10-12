using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Base abstraction for all executable commands in the platform.
/// All operations (atomic or composite) implement this interface.
/// </summary>
/// <remarks>
/// This is the foundational abstraction for the unified command model:
/// - Atomic commands: DataCommand (Query, Insert, Update, Delete)
/// - Composite commands: PipelineDefinition (stages that are commands)
///
/// Every executable operation in the system implements this interface,
/// enabling uniform execution, validation, and result handling.
/// </remarks>
public interface ICommand
{
    /// <summary>
    /// Gets the unique identifier for this command.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Gets the type of command (e.g., "Query", "Pipeline", "Transform").
    /// Used for routing to appropriate executors.
    /// </summary>
    string CommandType { get; }

    /// <summary>
    /// Gets whether this command is enabled for execution.
    /// Disabled commands are skipped without error.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Validates the command definition.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the command is valid.</returns>
    Task<IGenericResult> Validate(CancellationToken cancellationToken = default);
}