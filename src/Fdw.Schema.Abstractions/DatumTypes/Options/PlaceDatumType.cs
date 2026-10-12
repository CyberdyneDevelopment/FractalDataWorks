using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Where a record is: a district, a zip code, a state, or a longitude or latitude.</summary>
/// <remarks>
/// One type for anything that says where, because a place is not a measure: summing longitudes or zip
/// codes means nothing, so it is never aggregated. It is grouped by (a chart of values per district) and
/// plotted (a map binds one place field, or a longitude and a latitude). It takes the range operators because
/// a coordinate is filtered by a range (a map's bounding box) as a name is by equality. Which chart slot takes
/// it is the chart type's decision, not this type's.
/// </remarks>
[TypeOption(typeof(DatumTypes), "Place")]
[ExcludeFromCodeCoverage]
public sealed class PlaceDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlaceDatumType"/> class.</summary>
    public PlaceDatumType()
        : base(
            name: "Place",
            description: "Where a record is: a district, a zip code, a state, a longitude or a latitude.",
            isKeyRole: false,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }

    /// <inheritdoc />
    /// <remarks>A place is groupable like a category, but a chart places it on a map, so it sits on Place alone.</remarks>
    public override IReadOnlyList<string> Axes => [AxisTypes.Place.Name];
}
