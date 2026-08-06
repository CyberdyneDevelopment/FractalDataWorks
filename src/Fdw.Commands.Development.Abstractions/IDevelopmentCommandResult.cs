using System.Collections.Generic;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Represents the result of executing a development command.
/// </summary>
public interface IDevelopmentCommandResult
{
    /// <summary>
    /// Gets a human-readable summary of the result.
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Gets the structured data from the command execution.
    /// </summary>
    IReadOnlyDictionary<string, object> Data { get; }
}
