using System;
using System.Collections.Generic;

namespace Fdw.Workspace.Management;

/// <summary>
/// Represents a snapshot record within a session.
/// </summary>
public sealed class SnapshotRecord
{
    /// <summary>
    /// Gets or sets the unique snapshot identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the snapshot name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the snapshot description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the document changes in this snapshot.
    /// </summary>
    /// <remarks>
    /// Changes are stored as document path → content pairs for documents
    /// that differ from the on-disk state (baseline).
    /// </remarks>
    public IDictionary<string, string> DocumentChanges { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}