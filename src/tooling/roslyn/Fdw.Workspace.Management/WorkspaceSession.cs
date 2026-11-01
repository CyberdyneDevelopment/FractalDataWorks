using System;
using System.Collections.Generic;

namespace Fdw.Workspace.Management;

/// <summary>
/// Represents a serializable workspace session for persistence.
/// </summary>
/// <remarks>
/// A workspace session captures the state needed to restore a workspace
/// after the application has been closed or the connection has been lost.
/// </remarks>
public sealed class WorkspaceSession
{
    /// <summary>
    /// Gets or sets the unique session identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the original workspace identifier.
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the solution file.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the solution name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the session was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the session was last saved.
    /// </summary>
    public DateTimeOffset SavedAt { get; set; }

    /// <summary>
    /// Gets or sets the snapshot data.
    /// </summary>
    /// <remarks>
    /// Snapshots are stored as serializable delta records rather than full solution state.
    /// </remarks>
    public IList<SnapshotRecord> Snapshots { get; set; } = [];

    /// <summary>
    /// Gets or sets the baseline snapshot name, if set.
    /// </summary>
    public string? BaselineSnapshot { get; set; }

    /// <summary>
    /// Gets or sets optional metadata for the session.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the session version for migration support.
    /// </summary>
    public int Version { get; set; } = 1;
}