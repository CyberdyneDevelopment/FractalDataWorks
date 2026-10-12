using System.Collections.Generic;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>
/// Represents a discovered database schema (path).
/// </summary>
public sealed class DiscoveredPath
{
    /// <summary>
    /// Gets the schema name (e.g., "dbo", "conn", "sec").
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets the containers discovered in this schema.
    /// </summary>
    public required IReadOnlyList<DiscoveredContainer> Containers { get; init; }
}
