using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that calculates the sum of all values.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Sum", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SumAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SumAggregationType"/> class.
    /// </summary>
    public SumAggregationType() : base(9, "Sum", values => values.Sum())
    { }
}
