using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Functions;

/// <summary>
/// Sum aggregation function - sums all values.
/// </summary>
[TypeOption(typeof(AggregationFunctions), "Sum", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SumAggregationFunction : AggregationFunctionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SumAggregationFunction"/> class.
    /// </summary>
    public SumAggregationFunction() : base(id: 1, name: "Sum")
    {
    }

    /// <inheritdoc/>
    public override decimal Aggregate(IReadOnlyList<decimal> values)
    {
        return values.Sum();
    }
}
