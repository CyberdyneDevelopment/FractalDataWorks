using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Represents a composite data command that orchestrates multiple child commands.
/// A composite command sequences atomic commands together, managing data flow and error handling.
/// </summary>
/// <remarks>
/// <para>
/// Composite commands enable:
/// <list type="bullet">
/// <item>Sequential execution of multiple commands</item>
/// <item>Data flowing from one command output to the next input</item>
/// <item>Shared error handling and retry policies</item>
/// <item>Nested composition (composite commands can contain other composites)</item>
/// <item>Compensation handlers for rollback on failure</item>
/// </list>
/// </para>
/// <para>
/// Example: ETL Pipeline
/// <code>
/// ICompositeDataCommand pipeline = new PipelineDefinition
/// {
///     Commands = [
///         new QueryCommand("SourceTable"),           // Extract
///         new TransformCommand("MapFields"),         // Transform
///         new InsertCommand("DestinationTable")      // Load
///     ]
/// };
/// </code>
/// </para>
/// </remarks>
public interface ICompositeDataCommand : IDataCommand
{
    /// <summary>
    /// Gets the child commands that make up this composite command.
    /// Commands are executed in order; output of one command can feed into the next.
    /// </summary>
    IReadOnlyList<IDataCommand> Commands { get; }

    /// <summary>
    /// Gets the execution strategy for this composite command.
    /// </summary>
    ExecutionStrategyBase ExecutionStrategy { get; }

    /// <summary>
    /// Gets the error handling configuration for this composite command.
    /// </summary>
    ICompositeErrorHandling? ErrorHandling { get; }

    /// <summary>
    /// Gets the compensation handler that executes on failure to rollback changes.
    /// </summary>
    ICompensationHandler? CompensationHandler { get; }
}