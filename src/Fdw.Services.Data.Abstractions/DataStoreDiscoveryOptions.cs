using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Options for DataStore schema discovery.
/// </summary>
public sealed class DataStoreDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the schemas to exclude from discovery.
    /// Default: sys, INFORMATION_SCHEMA, guest
    /// </summary>
    public IReadOnlyList<string> ExcludedSchemas { get; set; } = new List<string>
    {
        "sys", "INFORMATION_SCHEMA", "guest"
    };

    /// <summary>
    /// Gets or sets the schemas to include (if specified, only these will be discovered).
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - required for config binding
    public IReadOnlyList<string> IncludeOnlySchemas { get; set; } = null!;
#pragma warning restore CA2227

    /// <summary>
    /// Gets or sets whether to discover views in addition to tables.
    /// Default: true
    /// </summary>
    public bool DiscoverViews { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to discover indexes.
    /// Default: true
    /// </summary>
    public bool DiscoverIndexes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to discover foreign key constraints.
    /// Default: true
    /// </summary>
    public bool DiscoverForeignKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to discover descriptions/extended properties.
    /// Default: true
    /// </summary>
    public bool DiscoverDescriptions { get; set; } = true;

    /// <summary>
    /// Creates default options.
    /// </summary>
    public static DataStoreDiscoveryOptions Default => new DataStoreDiscoveryOptions();
}