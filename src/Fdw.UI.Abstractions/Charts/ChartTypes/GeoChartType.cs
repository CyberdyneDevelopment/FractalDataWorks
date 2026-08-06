using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Geographic map — choropleth (region-based) or point map (lat/long-based).
/// </summary>
/// <remarks>
/// Either <c>Region</c> (choropleth) or the <c>Latitude</c> + <c>Longitude</c> pair
/// (point map) must be bound; the renderer selects the mode based on which encodings are present.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Geo")]
public sealed class GeoChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeoChartType"/> class.
    /// </summary>
    public GeoChartType()
        : base(
            id: 10,
            name: "Geo",
            displayName: "Map",
            category: "Spatial",
            iconHint: "map",
            requiredEncodings: ["Measure"],
            optionalEncodings: ["Region", "Latitude", "Longitude", "Color", "Size", "Tooltip"])
    {
    }
}
