namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Catalog search request.
/// </summary>
public sealed class CatalogSearchPayload
{
    /// <summary>Gets or sets the search query string.</summary>
    public string Query { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional entity type filter.</summary>
    public string? EntityType { get; set; }
}
