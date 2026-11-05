using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Options for DataStore schema discovery.
/// </summary>
/// <remarks>
/// Every choice is required (FDW-3823): the request that asks for the discovery states each, through
/// <c>DataStoreDiscoveryChoices.FromRequest</c> in Fdw.Services.Data, and nothing stands in for one it leaves out.
/// </remarks>
public sealed class DataStoreDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the schemas to exclude from discovery.
    /// </summary>
    public required IReadOnlyList<string> ExcludedSchemas { get; set; }

    /// <summary>
    /// Gets or sets the schemas to include; null reads every schema that is not excluded.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - required for config binding
    public IReadOnlyList<string>? IncludeOnlySchemas { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// Gets or sets whether to discover views in addition to tables.
    /// </summary>
    public required bool DiscoverViews { get; set; }

    /// <summary>
    /// Gets or sets whether to discover indexes.
    /// </summary>
    public required bool DiscoverIndexes { get; set; }

    /// <summary>
    /// Gets or sets whether to discover foreign key constraints.
    /// </summary>
    public required bool DiscoverForeignKeys { get; set; }

    /// <summary>
    /// Gets or sets whether to discover descriptions/extended properties.
    /// </summary>
    public required bool DiscoverDescriptions { get; set; }
}