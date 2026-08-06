using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Functions;

/// <summary>
/// Minimum aggregation function - finds the smallest value.
/// </summary>
[TypeOption(typeof(AggregationFunctions), "Min", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MinAggregationFunction : AggregationFunctionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinAggregationFunction"/> class.
    /// </summary>
    public MinAggregationFunction() : base(id: 3, name: "Min")
    {
    }

    /// <inheritdoc/>
    public override decimal Aggregate(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return 0;

        return values.Min();
    }
}
