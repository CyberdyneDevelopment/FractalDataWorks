using System;
using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Represents a single search result entry.
/// </summary>
public sealed class SearchResultPayload
{
    /// <summary>
    /// Gets or sets the entity type of the search result.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the matched entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the matched entity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the field that matched the search query.
    /// </summary>
    public string? MatchedField { get; set; }

    /// <summary>
    /// Gets or sets the URL to navigate to the matched entity.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata associated with the search result.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
