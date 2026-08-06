using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Visual style descriptor for a single canvas edge type in the SVG renderer.
/// </summary>
/// <remarks>
/// Keyed by the canvas edge type name (Ordinal) via <see cref="SvgEdgeAppearanceMap"/>.
/// </remarks>
/// <param name="StrokeColor">CSS colour string for the edge stroke.</param>
/// <param name="StrokeWidth">Stroke width in SVG user-units.</param>
/// <param name="DashArray">Dash-array string (e.g. "4 2") or empty for a solid stroke.</param>
/// <param name="MarkerRef">
/// The <c>marker-end</c> attribute value referencing the arrowhead marker defined in the
/// renderer's <c>&lt;defs&gt;</c> — e.g. <c>url(#fdw-svg-arrow-flow)</c>. Carried here so the
/// arrowhead colour stays with the stroke colour it must match, and so the renderer looks the
/// marker up through this map like every other edge visual instead of branching on the type name.
/// </param>
[ExcludeFromCodeCoverage]
internal sealed record EdgeAppearance(
    string StrokeColor,
    double StrokeWidth,
    string DashArray,
    string MarkerRef);
