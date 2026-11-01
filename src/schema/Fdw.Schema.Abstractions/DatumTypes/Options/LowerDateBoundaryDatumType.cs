using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Where a status or dimension record starts to hold: the lower edge of its validity period (effective from).</summary>
/// <remarks>The record holds at a moment t when this is at or before t and its UpperDateBoundary is after t. Not the time an event happened; that is Date.</remarks>
[TypeOption(typeof(DatumTypes), "LowerDateBoundary")]
[ExcludeFromCodeCoverage]
public sealed class LowerDateBoundaryDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LowerDateBoundaryDatumType"/> class.</summary>
    public LowerDateBoundaryDatumType()
        : base(
            name: "LowerDateBoundary",
            description: "Where a record starts to hold: the start of its validity period.",
            isKeyRole: false,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: true,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }
}
