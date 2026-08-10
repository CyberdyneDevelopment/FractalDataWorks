using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Client-side request for cross-field find within a container.
/// </summary>
public sealed class FindRequest
{
    /// <summary>
    /// Gets or sets the DataStore name.
    /// </summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path name within the DataStore.
    /// </summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field names to search within.
    /// </summary>
    public IList<string>? FieldNames { get; set; }

    /// <summary>
    /// Gets or sets whether the search is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the maximum results.
    /// </summary>
    public int MaxResults { get; set; } = 50;
}
