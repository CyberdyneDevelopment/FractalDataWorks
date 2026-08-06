using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that counts the number of values.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Count", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CountAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountAggregationType"/> class.
    /// </summary>
    public CountAggregationType() : base(2, "Count", values => values.Count)
    { }
}
