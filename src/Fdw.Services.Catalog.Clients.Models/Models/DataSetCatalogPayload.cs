using System;

namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// DataSet catalog entry.
/// </summary>
public sealed class DataSetCatalogPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
}
