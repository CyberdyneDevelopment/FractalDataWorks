using System;
using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Represents the response from a search query, including results and metadata.
/// </summary>
public sealed class SearchResponse
{
    /// <summary>
    /// Gets or sets the original search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of matching results.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the facet counts grouped by category.
    /// </summary>
    public IDictionary<string, int> Facets { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the list of search results.
    /// </summary>
    public IList<SearchResultPayload> Results { get; set; } = [];

    /// <summary>
    /// Gets or sets the search duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }
}
