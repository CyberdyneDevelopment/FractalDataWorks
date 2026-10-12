namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Data returned from snapshot operations.
/// </summary>
public sealed class SnapshotData
{
    /// <summary>
    /// Gets or sets the snapshot identifier.
    /// </summary>
    public string SnapshotId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the snapshot name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the snapshot description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of projects in the snapshot.
    /// </summary>
    public int ProjectCount { get; init; }

    /// <summary>
    /// Gets or sets the number of documents in the snapshot.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the snapshot was restored.
    /// </summary>
    public bool Restored { get; init; }
}
