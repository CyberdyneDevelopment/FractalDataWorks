using System.Collections.Generic;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Represents a discovered database schema (path).
/// </summary>
public sealed class DiscoveredPath
{
    /// <summary>
    /// Gets the schema name (e.g., "public", "app").
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets the containers discovered in this schema.
    /// </summary>
    public required IReadOnlyList<DiscoveredContainer> Containers { get; init; }
}
