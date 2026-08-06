using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// A single port together with its computed position, expressed as an offset from the owning
/// node's centre in canvas units.
/// </summary>
/// <remarks>
/// Offsets are position-independent — they depend only on the node's port list and appearance,
/// never on the node's X/Y. This is what lets a node drag translate its whole group (ports and
/// field-mapping edges included) without recomputing any port geometry.
/// </remarks>
/// <param name="Port">The port this placement positions.</param>
/// <param name="Dx">The horizontal offset from the node centre (negative = In column, positive = Out column).</param>
/// <param name="Dy">The vertical offset from the node centre.</param>
[ExcludeFromCodeCoverage]
internal sealed record PortPlacement(
    ICanvasPort Port,
    double Dx,
    double Dy);
