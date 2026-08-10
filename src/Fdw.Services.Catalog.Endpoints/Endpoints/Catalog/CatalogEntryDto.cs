using System;
using System.Collections.Generic;
namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Data transfer object representing a catalog entry returned from search.</summary>
public class CatalogEntryDto
{
    /// <summary>Gets or sets the type of the catalog entity.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the catalog entity.</summary>
    // Why: named 'Name' (not 'EntityName') so the JSON contract matches the client CatalogEntityPayload.Name
    // the UI binds — otherwise the catalog list renders blank names (client/server field-name drift).
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the catalog entity.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the owner of the catalog entity.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the tags associated with the catalog entity.</summary>
    public IList<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the date and time the catalog entity was last modified.</summary>
    public DateTime LastModified { get; set; }
}