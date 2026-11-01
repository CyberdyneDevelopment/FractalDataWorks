using System.Collections.Generic;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// The body of a query over a configuration resource: the query, the fields wanted back, and whether
/// a total is wanted.
/// </summary>
/// <remarks>
/// A data set's route takes neither extra: no data set strategy honours a projection, so accepting one
/// there would return columns the caller did not ask for. A resource's list is read from one table
/// whose fields the resource declares, so both can be honoured.
/// </remarks>
public class ResourceQueryRequest : QueryRequest
{
    /// <summary>
    /// Gets or sets the fields to return for each row, by the names the resource declares. Null or
    /// empty for every declared field.
    /// </summary>
    public IReadOnlyList<string?>? Fields { get; set; }

    /// <summary>
    /// Gets or sets whether the response carries how many rows the filter selects in all. A count is a
    /// second query, so it is made only when asked for.
    /// </summary>
    public bool IncludeTotal { get; set; }
}
