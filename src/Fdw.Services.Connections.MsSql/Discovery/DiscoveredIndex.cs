using System.Collections.Generic;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>
/// Represents a discovered index.
/// </summary>
public sealed class DiscoveredIndex
{
    /// <summary>
    /// Gets the index name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the columns in the index.
    /// </summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>
    /// Gets whether the index is unique.
    /// </summary>
    public required bool IsUnique { get; init; }

    /// <summary>
    /// Gets whether this is the primary key index.
    /// </summary>
    public required bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets whether this is a clustered index.
    /// </summary>
    public required bool IsClustered { get; init; }
}
