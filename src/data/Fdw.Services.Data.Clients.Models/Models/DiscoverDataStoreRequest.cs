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

    /// <summary>Gets or sets schemas to exclude from discovery.</summary>
    public IList<string>? ExcludedSchemas { get; set; }

    /// <summary>Gets or sets schemas to include exclusively during discovery.</summary>
    public IList<string>? IncludeOnlySchemas { get; set; }

    /// <summary>Gets or sets whether to discover views. Defaults to true.</summary>
    public bool DiscoverViews { get; set; } = true;

    /// <summary>Gets or sets whether to discover indexes. Defaults to true.</summary>
    public bool DiscoverIndexes { get; set; } = true;
}
