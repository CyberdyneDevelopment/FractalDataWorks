using System;

namespace Fdw.Abstractions;

/// <summary>
/// Base interface for all commands in the Fdw framework.
/// </summary>
/// <remarks>
/// This interface is in Fdw.Abstractions to avoid circular dependencies
/// with source generators, following the same pattern as IGenericService.
/// All framework commands should extend this interface to ensure consistent
/// behavior and integration with the command execution infrastructure.
/// </remarks>
public interface IGenericCommand
{
    /// <summary>
    /// Gets the unique identifier for this command instance.
    /// </summary>
    /// <value>A unique identifier for the command instance.</value>
    /// <remarks>
    /// This identifier is used for command tracking, logging, correlation, and debugging purposes.
    /// It should remain constant for the lifetime of the command instance.
    /// </remarks>
    Guid CommandId { get; }

    /// <summary>
    /// Gets the timestamp when this command was created.
    /// </summary>
    /// <value>The UTC timestamp of command creation.</value>
    /// <remarks>
    /// Used for auditing, performance measurement, and timeout calculations.
    /// Should be set at command instantiation and never modified.
    /// </remarks>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the command type identifier.
    /// </summary>
    /// <value>A string identifying the command type.</value>
    /// <remarks>
    /// This identifier is used for routing, logging, and metrics.
    /// Typically matches the concrete command class name or a descriptive identifier.
    /// </remarks>
    string CommandType { get; }

    /// <summary>
    /// Gets the command category for classification and routing.
    /// </summary>
    /// <value>A string identifying the command category (e.g., "Query", "Mutation", "Notification").</value>
    string Category { get; }
}
