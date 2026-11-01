using System;

namespace Fdw.Services.Abstractions.Commands;

/// <summary>
/// Interface for command type-specific metrics.
/// Provides detailed performance information for specific types of data commands.
/// </summary>
/// <remarks>
/// Command type metrics enable fine-grained performance analysis and optimization
/// by breaking down provider performance by the types of operations being performed.
/// </remarks>
public interface IGenericCommandTypeMetrics
{
    /// <summary>
    /// Gets the command type these metrics represent.
    /// </summary>
    /// <value>The command type name (e.g., "Query", "Insert", "Update").</value>
    string CommandType { get; }

    /// <summary>
    /// Gets the total number of commands of this type executed.
    /// </summary>
    /// <value>The total execution count for this command type.</value>
    long ExecutionCount { get; }

    /// <summary>
    /// Gets the number of successful executions for this command type.
    /// </summary>
    /// <value>The count of successful executions.</value>
    long SuccessCount { get; }

    /// <summary>
    /// Gets the number of failed executions for this command type.
    /// </summary>
    /// <value>The count of failed executions.</value>
    long FailureCount { get; }

    /// <summary>
    /// Gets the average execution time for this command type.
    /// </summary>
    /// <value>The average execution time, or null if no commands have been executed.</value>
    TimeSpan? AverageExecutionTime { get; }

    /// <summary>
    /// Gets the minimum execution time observed for this command type.
    /// </summary>
    /// <value>The fastest execution time, or null if no commands have been executed.</value>
    TimeSpan? MinExecutionTime { get; }

    /// <summary>
    /// Gets the maximum execution time observed for this command type.
    /// </summary>
    /// <value>The slowest execution time, or null if no commands have been executed.</value>
    TimeSpan? MaxExecutionTime { get; }

    /// <summary>
    /// Gets the timestamp of the last execution for this command type.
    /// </summary>
    /// <value>The UTC timestamp of the most recent command execution, or null if none have been executed.</value>
    DateTimeOffset? LastExecuted { get; }
}
