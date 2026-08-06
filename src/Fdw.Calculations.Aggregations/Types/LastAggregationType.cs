using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that returns the last value in the sequence.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Last", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LastAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LastAggregationType"/> class.
    /// </summary>
    public LastAggregationType() : base(4, "Last", values => values.Count == 0 ? 0m : values[values.Count - 1])
    { }
}
