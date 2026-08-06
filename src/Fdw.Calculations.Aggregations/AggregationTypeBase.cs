using System;
using System.Collections.Generic;
using Fdw.Calculations.Abstractions;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Base class for aggregation types.
/// Bridges the func-based CalculationTypeBase with decimal-list aggregation logic.
/// </summary>
public abstract class AggregationTypeBase : CalculationTypeBase, IAggregationType
{
    /// <summary>
    /// Protected parameterless constructor for TypeCollection Empty sentinel.
    /// Not for use in application code.
    /// </summary>
    protected AggregationTypeBase() : base()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregationTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregation type.</param>
    /// <param name="name">The name of this aggregation type.</param>
    /// <param name="calculate">Decimal-list aggregation func used for in-memory execution.</param>
    protected AggregationTypeBase(int id, string name, Func<IReadOnlyList<decimal>, decimal> calculate)
        : base(
            id: id,
            name: name,
            category: "Aggregation",
            calculate: (rows, col, _) => calculate(ExtractValues(rows, col)),
            toSql: (col, _) => string.Empty)
    { }

    private static List<decimal> ExtractValues(IReadOnlyList<IDataRow> rows, string col)
    {
        var values = new List<decimal>(rows.Count);
        foreach (var r in rows)
            if (r.TryGetValue<decimal>(col, out var v)) values.Add(v);
        return values;
    }
}
