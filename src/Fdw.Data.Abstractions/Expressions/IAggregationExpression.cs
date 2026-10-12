using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for aggregation expressions (GROUP BY clause representation).
/// </summary>
/// <remarks>
/// Represents grouping and aggregation functions.
/// Translators convert this to SQL GROUP BY/HAVING, aggregation logic, etc.
/// Note: Full aggregation support will be implemented in future phases.
/// </remarks>
public interface IAggregationExpression
{
    /// <summary>
    /// Gets the fields to group by.
    /// </summary>
    /// <value>A collection of property names to group by.</value>
    IReadOnlyList<string> GroupByFields { get; }

    /// <summary>
    /// Gets the aggregation functions to apply.
    /// </summary>
    /// <value>
    /// A dictionary mapping result field names to aggregation specifications.
    /// Example: { "TotalSales": "SUM(Amount)", "CustomerCount": "COUNT(*)" }
    /// </value>
    IReadOnlyDictionary<string, string> Aggregations { get; }

    /// <summary>
    /// Gets a value indicating whether the result also carries how many groups the query forms in all, in a
    /// column named <c>FdwGroupCount</c> on every row.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to ask for it. It is the count of groups before any paging, so a caller reading
    /// the top groups can say how many there are.
    /// </value>
    bool IncludeGroupCount { get; }
}
