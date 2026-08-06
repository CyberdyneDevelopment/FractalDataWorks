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
}
