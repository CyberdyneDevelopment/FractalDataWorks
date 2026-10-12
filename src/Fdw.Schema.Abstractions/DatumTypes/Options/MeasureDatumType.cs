using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A number that is summed, averaged or otherwise aggregated: a quantity, an amount, a count.</summary>
/// <remarks>Compared by range and aggregated; never grouped by.</remarks>
[TypeOption(typeof(DatumTypes), "Measure")]
[ExcludeFromCodeCoverage]
public sealed class MeasureDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MeasureDatumType"/> class.</summary>
    public MeasureDatumType()
        : base(
            name: "Measure",
            description: "A number that is summed or averaged: a quantity, an amount.",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: true,
            isOrderable: true,
            isGroupable: false,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }
}
