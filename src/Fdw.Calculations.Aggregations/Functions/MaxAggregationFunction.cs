using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Functions;

/// <summary>
/// Maximum aggregation function - finds the largest value.
/// </summary>
[TypeOption(typeof(AggregationFunctions), "Max", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxAggregationFunction : AggregationFunctionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAggregationFunction"/> class.
    /// </summary>
    public MaxAggregationFunction() : base(id: 4, name: "Max")
    {
    }

    /// <inheritdoc/>
    public override decimal Aggregate(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return 0;

        return values.Max();
    }
}
