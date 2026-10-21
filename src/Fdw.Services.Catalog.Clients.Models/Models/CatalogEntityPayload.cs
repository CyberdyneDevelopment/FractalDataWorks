using System;

namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Catalog entity summary.
/// </summary>
public sealed class CatalogEntityPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the entity name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of entity.</summary>
    public string EntityType { get; set; } = string.Empty;
    /// <summary>Gets or sets the description of the entity.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets where the entity sits (the data set of a field, the "store / path" of a container).</summary>
    public string? Location { get; set; }
}
