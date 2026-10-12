using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A point in time that describes the record: when an event happened, when someone joined, the hour something ran.</summary>
/// <remarks>
/// Where a status or dimension record is valid over a period instead, its edges are LowerDateBoundary and
/// UpperDateBoundary. At any precision the column records: a day, an hour, an instant. Ordered and compared
/// by range. A date stored as a yyyymmdd integer is still a Date: the encoding
/// is not the meaning (FDW-988 retired a separate DateInt type).
/// </remarks>
[TypeOption(typeof(DatumTypes), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateDatumType"/> class.</summary>
    public DateDatumType()
        : base(
            name: "Date",
            description: "A point in time that describes the record: an event, a join date, an hour.",
            isKeyRole: false,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: true,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }
}
