using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that finds the minimum value.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Min", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MinAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinAggregationType"/> class.
    /// </summary>
    public MinAggregationType() : base(7, "Min", values => values.Count == 0 ? 0m : values.Min())
    { }
}
