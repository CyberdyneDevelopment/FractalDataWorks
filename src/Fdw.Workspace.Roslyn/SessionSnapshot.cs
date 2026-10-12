using System;
using System.Collections.Generic;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents a snapshot within a session.
/// </summary>
public sealed record SessionSnapshot
{
    /// <summary>
    /// Gets the unique identifier for the snapshot.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the snapshot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets when the snapshot was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the document changes at the time of the snapshot.
    /// </summary>
    public IDictionary<string, string> DocumentChanges { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}