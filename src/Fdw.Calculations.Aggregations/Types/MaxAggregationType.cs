using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that finds the maximum value.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Max", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAggregationType"/> class.
    /// </summary>
    public MaxAggregationType() : base(5, "Max", values => values.Count == 0 ? 0m : values.Max())
    { }
}
