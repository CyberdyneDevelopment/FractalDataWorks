using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Combines the filter a route puts on a query with the filter the caller asked for (FDW-1032).</summary>
internal static class DataQueryScope
{
    /// <summary>The caller's filter narrowed by the scope: both, combined with AND, or whichever one there is.</summary>
    /// <param name="scope">The filter the route puts on the data set (a view's, say), or null for none.</param>
    /// <param name="requested">The filter in the caller's body, or null for none.</param>
    /// <returns>The filter to translate; null when neither side has one.</returns>
    internal static QueryFilterNode? Combine(QueryFilterNode? scope, QueryFilterNode? requested)
        => scope is null
            ? requested
            : requested is null
                ? scope
                : new QueryFilterNode { Logic = "And", Nodes = new List<QueryFilterNode?> { scope, requested } };
}
