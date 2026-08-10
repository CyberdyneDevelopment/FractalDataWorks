using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Represents a search request with query parameters and filters.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>
    /// Gets or sets the search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity types to filter search results by.
    /// </summary>
    public IList<string>? Types { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
