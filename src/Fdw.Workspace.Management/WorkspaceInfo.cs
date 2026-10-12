using System;

namespace Fdw.Workspace.Management;

/// <summary>
/// Provides information about a loaded workspace.
/// </summary>
public sealed class WorkspaceInfo
{
    /// <summary>
    /// Gets the unique identifier for this workspace.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the path to the solution file.
    /// </summary>
    public string SolutionPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the solution name (file name without extension).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of projects in the solution.
    /// </summary>
    public int ProjectCount { get; init; }

    /// <summary>
    /// Gets the time when the workspace was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; init; }

    /// <summary>
    /// Gets whether the workspace has unsaved changes.
    /// </summary>
    public bool HasChanges { get; init; }

    /// <summary>
    /// Gets the number of snapshots created for this workspace.
    /// </summary>
    public int SnapshotCount { get; init; }

    /// <summary>
    /// Gets whether a baseline has been set for this workspace.
    /// </summary>
    public bool HasBaseline { get; init; }
}
