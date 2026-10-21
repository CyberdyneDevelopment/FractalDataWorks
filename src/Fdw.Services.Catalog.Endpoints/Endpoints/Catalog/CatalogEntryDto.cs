using System;
using System.Collections.Generic;
namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Data transfer object representing a catalog entry returned from search.</summary>
public class CatalogEntryDto
{
    /// <summary>Gets or sets the kind of the catalog entity: one of <see cref="CatalogKinds"/>.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the catalog entity.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the catalog entity.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets where the entity sits: the data set of a data set field, the data store of a path, "store / path" for a
    /// container and "store / path / container" for a container field. Null for an entity that sits under nothing.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the names the entity sits under, outermost first (FDW-1697): the data store of a path, the store and path of a container,
    /// the store, path and container of a container field. Empty for an entity that sits under nothing. A screen opens the entity from these
    /// names and never takes <see cref="Location"/> apart.
    /// </summary>
    public IList<string> Parents { get; set; } = [];

    /// <summary>Gets or sets the owner of the catalog entity, for the kinds that record one.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the tags associated with the catalog entity.</summary>
    public IList<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the date and time the catalog entity was last modified, for the kinds that record one.</summary>
    public DateTime? LastModified { get; set; }
}
