using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request for <c>POST /data/{DataSetName}</c> — a query of one data set — and the body every layer
/// over it (a workspace) accepts as well.
/// </summary>
/// <remarks>
/// The query itself is the body every queryable route shares (<see cref="QueryRequest"/>); this adds
/// the one thing a data set's route has and a configuration resource's does not, the data set's name.
/// There is no field list: no DataSet strategy honours a caller's projection (Simple never passes one,
/// Compound builds its own), so accepting one would return columns the caller did not ask for.
/// <see cref="GroupBy"/> and <see cref="Aggregates"/> turn the query into a grouped one: the rows are the
/// groups, the filter applies before the grouping, <c>Sort</c> may name a group field or an aggregate's
/// <c>As</c> name, and <c>Skip</c>/<c>Take</c> page the groups (FDW-1023).
/// </remarks>
public class DataQueryRequest : QueryRequest
{
    /// <summary>Gets or sets the data set name (route).</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data set fields to group by, or null for an ungrouped query. Empty with
    /// <see cref="Aggregates"/> given reads one row: the aggregates over every row the filter keeps.
    /// </summary>
    public IReadOnlyList<string>? GroupBy { get; set; }

    /// <summary>Gets or sets the aggregates each group is read with, or null for none.</summary>
    public IReadOnlyList<DataQueryAggregate?>? Aggregates { get; set; }
}
