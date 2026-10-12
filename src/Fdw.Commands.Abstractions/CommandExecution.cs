using System;
using System.Collections.Generic;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Represents a command execution instance with tracking metadata.
/// Separates command TYPE (static singleton) from command EXECUTION (runtime instance).
/// </summary>
/// <remarks>
/// <para>
/// This class solves the confusion between command type definitions and command executions:
/// <list type="bullet">
/// <item><strong>Command Type</strong>: Static singleton (e.g., SqlQueryCommandType.Instance) - defines WHAT kind of command</item>
/// <item><strong>Command Execution</strong>: Runtime instance (this class) - represents a specific execution WITH tracking metadata</item>
/// </list>
/// </para>
/// <para>
/// Use this pattern:
/// <code>
/// // Get a payload with the data
/// var payload = new SqlQueryPayload { SqlText = "SELECT * FROM Users", Parameters = ... };
///
/// // Wrap in execution context
/// var execution = new CommandExecution(SqlQueryCommandType.Instance, payload);
///
/// // Execute
/// await service.Execute(execution);
/// </code>
/// </para>
/// </remarks>
public sealed record CommandExecution
{
    /// <summary>
    /// Gets the unique identifier for THIS execution instance.
    /// </summary>
    /// <value>A GUID that uniquely identifies this specific execution, for tracking and correlation.</value>
    public Guid ExecutionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets when this execution was created.
    /// </summary>
    /// <value>The UTC timestamp when this execution context was instantiated.</value>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the command type being executed (static singleton reference).
    /// </summary>
    /// <value>
    /// Reference to the singleton command type (e.g., SqlQueryCommandType.Instance)
    /// that defines the operation characteristics.
    /// </value>
    public IGenericCommandType CommandType { get; init; }

    /// <summary>
    /// Gets the execution payload (parameters, SQL text, HTTP body, etc.).
    /// </summary>
    /// <value>
    /// The command-specific data needed for execution. Type depends on the CommandType:
    /// <list type="bullet">
    /// <item>SqlQueryPayload for SQL queries</item>
    /// <item>HttpRequestPayload for HTTP commands</item>
    /// <item>etc.</item>
    /// </list>
    /// </value>
    public object? Payload { get; init; }

    /// <summary>
    /// Gets the optional correlation ID for tracking related executions.
    /// </summary>
    /// <value>
    /// A GUID that links multiple related command executions together
    /// (e.g., all commands in a single user request or transaction).
    /// </value>
    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// Gets optional metadata for this execution.
    /// </summary>
    /// <value>
    /// Additional key-value pairs for tracking context like user ID, tenant ID, etc.
    /// </value>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecution"/> class.
    /// </summary>
    /// <param name="commandType">The command type being executed (singleton reference).</param>
    /// <param name="payload">The execution payload (command-specific data).</param>
    public CommandExecution(IGenericCommandType commandType, object? payload = null)
    {
        CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        Payload = payload;
    }
}
