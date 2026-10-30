using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// The body of a query: a filter tree, an ordering and a page.
/// </summary>
/// <remarks>
/// Everything but the names travels in the body so a query is never limited by URL length and can
/// carry a filter tree rather than flat equality pairs. The same body serves a data set's rows and a
/// configuration resource's list; what differs is which fields a name may reach, and that is declared
/// by the resource, not by the body. Every member may be left out, and a null for any of them means the same as
/// leaving it out (FDW-1125).
/// </remarks>
public class QueryRequest
{
    /// <summary>Gets or sets the filter tree, or null for every row.</summary>
    public QueryFilterNode? Filter { get; set; }

    /// <summary>Gets or sets the ordering, first entry most significant. Null or empty for the store's order.</summary>
    public IReadOnlyList<QuerySort?>? Sort { get; set; }

    /// <summary>Gets or sets the number of rows to skip. Must not be negative. Absent or null skips none.</summary>
    [JsonConverter(typeof(NullAsOmittedInt32Converter))]
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the number of rows to return. When omitted, the <c>DefaultPaginationSize</c>
    /// setting applies; a value above the <c>MaxPaginationSize</c> setting is capped to it.
    /// </summary>
    public int? Take { get; set; }
}
