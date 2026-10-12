using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows a number at a place, given as one place field or as longitude and latitude.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Map")]
public sealed class MapChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapChartTypeOption"/> class.</summary>
    public MapChartTypeOption()
        : base("Map", "Shows a number at a place, given as one place field or as longitude and latitude.",
        [
            new ChartBindingShape("longitudeAndLatitude",
            [
                new ChartSlot("map.x", ["Place"], isRequired: true, allowsMany: false),
                new ChartSlot("map.y", ["Place"], isRequired: true, allowsMany: false),
                new ChartSlot("map.z", ["Measure"], isRequired: false, allowsMany: false),
            ]),
            new ChartBindingShape("onePlace",
            [
                new ChartSlot("map.place", ["Place"], isRequired: true, allowsMany: false),
                new ChartSlot("map.z", ["Measure"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
