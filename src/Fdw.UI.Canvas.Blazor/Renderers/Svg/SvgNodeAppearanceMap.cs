using System;
using System.Collections.Generic;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Data-driven lookup table that maps a canvas node type's <c>Name</c> to an
/// <see cref="NodeAppearance"/> descriptor used by the SVG renderer.
/// </summary>
/// <remarks>
/// <para>
/// Seeded once at construction from <c>CanvasNodeTypes.All()</c> supplemented by a
/// per-known-type colour/shape table. Unknown types receive the default appearance.
/// No switch/if-else on type names appears here or in the .razor file — the renderer
/// calls <see cref="Get"/> and uses the returned descriptor directly.
/// </para>
/// <para>
/// The seeded appearances match the Lineage.razor colour vocabulary so existing users
/// see a familiar palette.
/// </para>
/// </remarks>
internal sealed class SvgNodeAppearanceMap
{
    private const double BoxHalfWidth = 60.0;
    private const double ParallelogramHalfWidth = 55.0;
    private const double DiamondHalfWidth = 45.0;
    private const double HexagonHalfWidth = 40.0;
    private const double CircleHalfWidth = 28.0;

    private static readonly NodeAppearance _default =
        new(NodeShape.RoundedRect, "#e2e8f0", "#0f172a", BoxHalfWidth);

    private readonly Dictionary<string, NodeAppearance> _map =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes the appearance map from the seeded known-type table.
    /// Entries missing from the table receive the default rounded-rect appearance.
    /// </summary>
    public SvgNodeAppearanceMap()
    {
        Seed("Connection",    NodeShape.Rectangle,    "#9ca3af", "#0f172a", BoxHalfWidth);
        Seed("DataStore",     NodeShape.RoundedRect,  "#3b82f6", "#0f172a", BoxHalfWidth);
        Seed("DataSet",       NodeShape.Parallelogram,"#06b6d4", "#0f172a", ParallelogramHalfWidth);
        Seed("Calculation",   NodeShape.Circle,       "#f97316", "#0f172a", CircleHalfWidth);
        Seed("Transform",     NodeShape.Hexagon,      "#a855f7", "#0f172a", HexagonHalfWidth);
        Seed("Pipeline",      NodeShape.Diamond,      "#ef4444", "#0f172a", DiamondHalfWidth);
        Seed("Schedule",      NodeShape.RoundedRect,  "#22c55e", "#0f172a", BoxHalfWidth);
        Seed("CalcInput",     NodeShape.RoundedRect,  "#fbbf24", "#0f172a", BoxHalfWidth);
        Seed("CalcOperation", NodeShape.RoundedRect,  "#f97316", "#0f172a", BoxHalfWidth);
        Seed("CalcOutput",    NodeShape.RoundedRect,  "#34d399", "#0f172a", BoxHalfWidth);
    }

    private void Seed(string name, string shape, string stroke, string fill, double portAnchorHalfWidth) =>
        _map[name] = new NodeAppearance(shape, stroke, fill, portAnchorHalfWidth);

    /// <summary>
    /// Returns the appearance for the given node type name, or the default appearance
    /// for types not in the seeded table.
    /// </summary>
    public NodeAppearance Get(string nodeTypeName) =>
        _map.TryGetValue(nodeTypeName, out var appearance) ? appearance : _default;
}
