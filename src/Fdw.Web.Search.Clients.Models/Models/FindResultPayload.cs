using System.Collections.Generic;

namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// A single matched record from a find operation.
/// </summary>
public sealed class FindResultPayload
{
    /// <summary>
    /// Gets or sets the matched record data as key-value pairs.
    /// </summary>
    public IDictionary<string, object?> Record { get; set; } = new Dictionary<string, object?>(System.StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the field names that matched the search term.
    /// </summary>
    public IList<string> MatchedFields { get; set; } = [];
}
