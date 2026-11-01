using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Where a status or dimension record stops holding: the upper edge of its validity period (effective to).</summary>
/// <remarks>The record holds at a moment t when its LowerDateBoundary is at or before t and this is after t. Not the time an event happened; that is Date.</remarks>
[TypeOption(typeof(DatumTypes), "UpperDateBoundary")]
[ExcludeFromCodeCoverage]
public sealed class UpperDateBoundaryDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpperDateBoundaryDatumType"/> class.</summary>
    public UpperDateBoundaryDatumType()
        : base(
            name: "UpperDateBoundary",
            description: "Where a record stops holding: the end of its validity period.",
            isKeyRole: false,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: true,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }
}
