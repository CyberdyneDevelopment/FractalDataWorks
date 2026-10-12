using System;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Defines error handling behavior for composite commands.
/// </summary>
public interface ICompositeErrorHandling
{
    /// <summary>
    /// Gets the retry policy for failed command execution.
    /// </summary>
    IRetryPolicy? RetryPolicy { get; }

    /// <summary>
    /// Gets the timeout for individual command execution.
    /// </summary>
    TimeSpan? CommandTimeout { get; }

    /// <summary>
    /// Gets the timeout for the entire composite command.
    /// </summary>
    TimeSpan? CompositeTimeout { get; }

    /// <summary>
    /// Gets the logging level for command execution details.
    /// </summary>
    string LogLevel { get; }
}