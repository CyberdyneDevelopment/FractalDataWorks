using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Asks where a named thing is used, or what it uses (FDW-1683).</summary>
public class CatalogUsesRequest
{
    /// <summary>Gets or sets the kind of the thing, one of <see cref="CatalogKinds"/>.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the name of the thing.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the names the thing sits under, outermost first (the store and path of a container); left empty, any thing of that name matches.</summary>
    public IList<string> Parents { get; set; } = [];

    /// <summary>Gets or sets which way to read: <see cref="CatalogUsesDirections.UsedBy"/> for what uses the thing, <see cref="CatalogUsesDirections.Uses"/> for what it uses.</summary>
    public string? Direction { get; set; }
}
