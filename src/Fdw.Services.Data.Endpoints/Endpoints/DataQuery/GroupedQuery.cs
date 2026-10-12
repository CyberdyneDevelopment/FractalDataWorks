using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A grouped data query ready to dispatch, and the columns its rows carry.</summary>
/// <param name="Command">The command: the filter, the order, a page of groups and the aggregation.</param>
/// <param name="Columns">The group fields, then each aggregate, in the order the rows carry them.</param>
/// <param name="Plan">
/// For a plain read, the plan whose in-memory part is applied to the rows after they are read (FDW-1045); null for a grouped
/// query, which the store runs whole.
/// </param>
internal sealed record GroupedQuery(
    QueryCommand<Dictionary<string, object?>> Command,
    IReadOnlyList<DataQueryColumnDto> Columns,
    QueryPlan? Plan = null);
