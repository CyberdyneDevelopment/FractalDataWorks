using System.Collections.Generic;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Result of schema discovery operation containing all discovered paths and containers.
/// </summary>
public sealed class SchemaDiscoveryResult
{
    /// <summary>
    /// Gets the discovered database paths (schemas).
    /// </summary>
    public required IReadOnlyList<DiscoveredPath> Paths { get; init; }

    /// <summary>
    /// Gets the total number of containers discovered.
    /// </summary>
    public required int TotalContainers { get; init; }

    /// <summary>
    /// Gets the total number of fields discovered across all containers.
    /// </summary>
    public required int TotalFields { get; init; }

    /// <summary>
    /// Gets the database name that was discovered.
    /// </summary>
    public required string DatabaseName { get; init; }
}
