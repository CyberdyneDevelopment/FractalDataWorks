using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IAggregationExpression for GROUP BY clause representation.
/// </summary>
/// <remarks>
/// Full aggregation support will be implemented in future phases.
/// This provides basic structure for group by and aggregation functions.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AggregationExpression : IAggregationExpression
{
    /// <summary>The name of the column that carries the number of groups when <see cref="IncludeGroupCount"/> is set.</summary>
    public const string GroupCountColumn = "FdwGroupCount";

    /// <summary>
    /// Gets or sets the fields to group by.
    /// </summary>
    public required IReadOnlyList<string> GroupByFields { get; init; }

    /// <summary>
    /// Gets or sets the aggregation functions.
    /// Dictionary maps result field name to aggregation specification.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Aggregations { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether each row also carries the total number of groups, in
    /// <see cref="GroupCountColumn"/>.
    /// </summary>
    public bool IncludeGroupCount { get; init; }
}
