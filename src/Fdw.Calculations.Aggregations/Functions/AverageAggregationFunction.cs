using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Functions;

/// <summary>
/// Average aggregation function - calculates mean of all values.
/// </summary>
[TypeOption(typeof(AggregationFunctions), "Average", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AverageAggregationFunction : AggregationFunctionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageAggregationFunction"/> class.
    /// </summary>
    public AverageAggregationFunction() : base(id: 2, name: "Average")
    {
    }

    /// <inheritdoc/>
    public override decimal Aggregate(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return 0;

        return values.Average();
    }
}
