using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Well-known SVG shape identifiers used by the <see cref="SvgCanvasRenderer"/>
/// to render canvas nodes.
/// </summary>
/// <remarks>
/// String constants are used rather than an enum so that downstream packages can extend
/// the set of recognised shapes without requiring changes to this file (open/extensible).
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class NodeShape
{
    /// <summary>Rounded rectangle — default for most node types.</summary>
    public const string RoundedRect = "RoundedRect";

    /// <summary>Diamond / rotated square — for pipeline (processing) nodes.</summary>
    public const string Diamond = "Diamond";

    /// <summary>Circle — for calculation and simple scalar nodes.</summary>
    public const string Circle = "Circle";

    /// <summary>Parallelogram — for dataset / schema nodes.</summary>
    public const string Parallelogram = "Parallelogram";

    /// <summary>Hexagon — for transform / operation nodes.</summary>
    public const string Hexagon = "Hexagon";

    /// <summary>Sharp rectangle (no radius) — for infrastructure / connection nodes.</summary>
    public const string Rectangle = "Rectangle";
}
