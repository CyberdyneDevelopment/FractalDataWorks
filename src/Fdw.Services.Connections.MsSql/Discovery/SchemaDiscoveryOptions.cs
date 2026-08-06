using System.Collections.Generic;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>
/// Options for controlling schema discovery behavior.
/// </summary>
public sealed class SchemaDiscoveryOptions
{
    /// <summary>
    /// Gets or initializes the schemas to exclude from discovery.
    /// Default: sys, INFORMATION_SCHEMA, guest
    /// </summary>
    public IReadOnlyList<string> ExcludedSchemas { get; init; } =
        ["sys", "INFORMATION_SCHEMA", "guest"];

    /// <summary>
    /// Gets or initializes the schemas to include (if specified, only these will be discovered).
    /// If null or empty, all schemas except ExcludedSchemas are included.
    /// </summary>
    public IReadOnlyList<string>? IncludeOnlySchemas { get; init; }

    /// <summary>
    /// Gets or initializes table name patterns to exclude (using SQL LIKE syntax).
    /// </summary>
    public IReadOnlyList<string>? ExcludedTablePatterns { get; init; }

    /// <summary>
    /// Gets or initializes whether to discover views in addition to tables.
    /// Default: true
    /// </summary>
    public bool DiscoverViews { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether to discover indexes.
    /// Default: true
    /// </summary>
    public bool DiscoverIndexes { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether to discover foreign key constraints.
    /// Default: true
    /// </summary>
    public bool DiscoverForeignKeys { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether to discover extended properties (descriptions).
    /// Default: true
    /// </summary>
    public bool DiscoverDescriptions { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether to include system tables (tables starting with sys).
    /// Default: false
    /// </summary>
    public bool IncludeSystemTables { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of tables to discover per schema.
    /// Useful for limiting discovery in large databases. 0 = no limit.
    /// Default: 0 (no limit)
    /// </summary>
    public int MaxTablesPerSchema { get; init; }

    /// <summary>
    /// Creates default options suitable for most scenarios.
    /// </summary>
    public static SchemaDiscoveryOptions Default => new();

    /// <summary>
    /// Creates options that discover only tables (no views).
    /// </summary>
    public static SchemaDiscoveryOptions TablesOnly => new()
    {
        DiscoverViews = false
    };

    /// <summary>
    /// Creates options for discovering a specific schema.
    /// </summary>
    /// <param name="schemaName">The schema to discover.</param>
    public static SchemaDiscoveryOptions ForSchema(string schemaName) => new()
    {
        IncludeOnlySchemas = [schemaName]
    };
}
