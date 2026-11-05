using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request body for the POST <c>datastores/-/discover</c> endpoint. Serializes to the wire shape
/// the server's <c>DiscoverDataStoreRequest</c> binds (case-insensitive JSON property names).
/// </summary>
public sealed class DiscoverDataStoreRequest
{
    /// <summary>Gets or sets the data store name to discover.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to refresh cached schema information.</summary>
    public bool Refresh { get; set; }

    /// <summary>Gets or sets the schemas to leave out; an empty list leaves out none. Required (FDW-3823).</summary>
    public IList<string>? ExcludedSchemas { get; set; }

    /// <summary>Gets or sets schemas to include exclusively during discovery.</summary>
    public IList<string>? IncludeOnlySchemas { get; set; }

    /// <summary>Gets or sets whether views are read. Required (FDW-3823).</summary>
    public bool? DiscoverViews { get; set; }

    /// <summary>Gets or sets whether indexes are read. Required (FDW-3823).</summary>
    public bool? DiscoverIndexes { get; set; }

    /// <summary>Gets or sets whether foreign keys are read. Required (FDW-3823).</summary>
    public bool? DiscoverForeignKeys { get; set; }

    /// <summary>Gets or sets whether descriptions are read. Required (FDW-3823).</summary>
    public bool? DiscoverDescriptions { get; set; }
}
