using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request for syncing schema changes via the API client.
/// </summary>
public sealed class SyncSchemaRequest
{
    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to apply detected changes.
    /// </summary>
    public bool ApplyChanges { get; set; }

    /// <summary>Gets or sets the schemas a discovery leaves out; an empty list leaves out none. Required (FDW-3823).</summary>
    public IList<string>? ExcludedSchemas { get; set; }

    /// <summary>Gets or sets the only schemas a discovery reads; null reads every schema not left out.</summary>
    public IList<string>? IncludeOnlySchemas { get; set; }

    /// <summary>Gets or sets whether a discovery reads views. Required (FDW-3823).</summary>
    public bool? DiscoverViews { get; set; }

    /// <summary>Gets or sets whether a discovery reads indexes. Required (FDW-3823).</summary>
    public bool? DiscoverIndexes { get; set; }

    /// <summary>Gets or sets whether a discovery reads foreign keys. Required (FDW-3823).</summary>
    public bool? DiscoverForeignKeys { get; set; }

    /// <summary>Gets or sets whether a discovery reads descriptions. Required (FDW-3823).</summary>
    public bool? DiscoverDescriptions { get; set; }
}
