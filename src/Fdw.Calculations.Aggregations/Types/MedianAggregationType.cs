using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that calculates the median (middle) value.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Median", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MedianAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MedianAggregationType"/> class.
    /// </summary>
    public MedianAggregationType() : base(6, "Median", values =>
    {
        if (values.Count == 0) return 0m;
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        return sorted[count / 2];
    })
    { }
}
