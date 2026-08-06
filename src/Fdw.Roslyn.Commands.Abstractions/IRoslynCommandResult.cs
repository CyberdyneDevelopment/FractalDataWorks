using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marker interface for Roslyn command results.
/// </summary>
public interface IRoslynCommandResult
{
    /// <summary>
    /// Gets a summary of the result.
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents a mutation.
    /// </summary>
    bool IsMutation { get; }

    /// <summary>
    /// Gets the new solution if this is a mutation result, otherwise null.
    /// </summary>
    Solution? NewSolution { get; }
}
