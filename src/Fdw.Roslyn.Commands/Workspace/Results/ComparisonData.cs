using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Data returned from workspace comparison operations.
/// </summary>
public sealed class ComparisonData
{
    /// <summary>
    /// Gets or sets a value indicating whether a baseline has been set.
    /// </summary>
    public bool HasBaseline { get; init; }

    /// <summary>
    /// Gets or sets the number of changes detected.
    /// </summary>
    public int ChangeCount { get; init; }

    /// <summary>
    /// Gets or sets the list of changes detected.
    /// </summary>
    public IReadOnlyList<WorkspaceChange> Changes { get; init; } = System.Array.Empty<WorkspaceChange>();
}