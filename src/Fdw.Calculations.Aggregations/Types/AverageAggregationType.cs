using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that calculates the average (mean) of all values.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Average", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AverageAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageAggregationType"/> class.
    /// </summary>
    public AverageAggregationType() : base(1, "Average", values => values.Count == 0 ? 0m : values.Average())
    { }
}
