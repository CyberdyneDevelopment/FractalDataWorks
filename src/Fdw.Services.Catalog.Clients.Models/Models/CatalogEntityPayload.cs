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
}
