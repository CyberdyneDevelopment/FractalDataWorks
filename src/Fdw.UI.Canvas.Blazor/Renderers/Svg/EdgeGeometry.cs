using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// The resolved start and end points of a rendered canvas edge.
/// </summary>
/// <remarks>
/// The coordinate space depends on the caller: node-to-node edges resolve to absolute canvas
/// coordinates, while a node's self-loop field-mapping edges resolve to offsets relative to that
/// node's centre (they render inside the node's own translated group).
/// </remarks>
/// <param name="SourceX">The X coordinate of the edge's source endpoint.</param>
/// <param name="SourceY">The Y coordinate of the edge's source endpoint.</param>
/// <param name="TargetX">The X coordinate of the edge's target endpoint.</param>
/// <param name="TargetY">The Y coordinate of the edge's target endpoint.</param>
[ExcludeFromCodeCoverage]
internal sealed record EdgeGeometry(
    double SourceX,
    double SourceY,
    double TargetX,
    double TargetY);
