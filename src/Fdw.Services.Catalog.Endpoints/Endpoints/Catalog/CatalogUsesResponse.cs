using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Where a named thing is used, or what it uses (FDW-1683).</summary>
public sealed class CatalogUsesResponse
{
    /// <summary>Gets or sets the kind of the thing asked about.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the thing asked about.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the direction that was read: one of <see cref="CatalogUsesDirections"/>.</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Gets or sets the things found, by name within kind; an empty list means nothing uses it, not an error.</summary>
    public IList<CatalogUseDto> Items { get; set; } = [];

    /// <summary>Gets or sets how many things were found.</summary>
    public int Total { get; set; }
}
