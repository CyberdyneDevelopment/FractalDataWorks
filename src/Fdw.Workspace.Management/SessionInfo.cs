using System;

namespace Fdw.Workspace.Management;

/// <summary>
/// Provides information about a saved workspace session.
/// </summary>
public sealed class SessionInfo
{
    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the original workspace ID that was saved.
    /// </summary>
    public Guid OriginalWorkspaceId { get; init; }

    /// <summary>
    /// Gets the path to the solution file.
    /// </summary>
    public string SolutionPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the solution name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the time when the session was saved.
    /// </summary>
    public DateTimeOffset SavedAt { get; init; }

    /// <summary>
    /// Gets the number of snapshots included in the session.
    /// </summary>
    public int SnapshotCount { get; init; }

    /// <summary>
    /// Gets whether the session includes a baseline.
    /// </summary>
    public bool HasBaseline { get; init; }

    /// <summary>
    /// Gets optional metadata associated with the session.
    /// </summary>
    public string? Metadata { get; init; }
}
