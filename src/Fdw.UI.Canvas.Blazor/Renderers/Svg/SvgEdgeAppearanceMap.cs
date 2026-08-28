using System;
using System.Collections.Generic;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Data-driven lookup table that maps a canvas edge type's <c>Name</c> to an
/// <see cref="EdgeAppearance"/> descriptor used by the SVG renderer.
/// </summary>
internal sealed class SvgEdgeAppearanceMap
{
    private static readonly EdgeAppearance _default =
        new(StrokeColor: "#64748b", StrokeWidth: 1.5, DashArray: string.Empty, MarkerRef: "url(#fdw-svg-arrow)");

    private readonly Dictionary<string, EdgeAppearance> _map =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes the appearance map from the seeded known-type table.
    /// </summary>
    public SvgEdgeAppearanceMap()
    {
        Seed("Flow",         "#06b6d4", 1.5, string.Empty, "url(#fdw-svg-arrow-flow)");
        Seed("Reference",    "#64748b", 1.5, "4 2",        "url(#fdw-svg-arrow-reference)");
        Seed("FieldMapping", "#a855f7", 1.0, "2 2",        "url(#fdw-svg-arrow-fieldmapping)");
    }

    private void Seed(string name, string color, double width, string dash, string markerRef) =>
        _map[name] = new EdgeAppearance(color, width, dash, markerRef);

    /// <summary>
    /// Returns the appearance for the given edge type name, or the default appearance.
    /// </summary>
    public EdgeAppearance Get(string edgeTypeName) =>
        _map.TryGetValue(edgeTypeName, out var appearance) ? appearance : _default;
}
