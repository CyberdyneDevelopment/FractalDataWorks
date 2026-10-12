using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Client-side response for cross-field find within a container.
/// </summary>
public sealed class FindResponse
{
    /// <summary>
    /// Gets or sets the search term that was executed.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container that was searched.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total result count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the matched records.
    /// </summary>
    public IList<FindResultPayload> Results { get; set; } = [];

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }
}
