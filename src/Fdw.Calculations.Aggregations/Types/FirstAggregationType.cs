using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that returns the first value in the sequence.
/// </summary>
[TypeOption(typeof(AggregationTypes), "First", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FirstAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FirstAggregationType"/> class.
    /// </summary>
    public FirstAggregationType() : base(3, "First", values => values.Count == 0 ? 0m : values[0])
    { }
}
