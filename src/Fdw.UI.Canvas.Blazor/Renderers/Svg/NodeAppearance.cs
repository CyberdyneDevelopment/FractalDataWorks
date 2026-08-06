using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Visual style descriptor for a single canvas node type in the SVG renderer.
/// </summary>
/// <remarks>
/// Keyed by the canvas node type name (Ordinal) via <see cref="SvgNodeAppearanceMap"/>.
/// Keeping shape metadata here avoids any switch/if-else on type names in the .razor file.
/// </remarks>
/// <param name="Shape">
/// Shape identifier string. One of the <see cref="NodeShape"/> constants, or a custom string
/// value for renderer extensions.
/// </param>
/// <param name="StrokeColor">CSS colour string used for node border stroke and label text.</param>
/// <param name="FillColor">CSS colour string used for the node fill (interior).</param>
/// <param name="PortAnchorHalfWidth">
/// Horizontal offset from the node centre at which this type's ports are drawn. It tracks the
/// half-width of the type's <paramref name="Shape"/> so ports land on the shape's own edge rather
/// than floating off a narrow silhouette — a Circle's ports sit at its radius, a Hexagon's on its
/// waist. Ports use <paramref name="StrokeColor"/> for their rim and <paramref name="FillColor"/>
/// for their interior, so they read as part of the node without duplicating its palette here.
/// </param>
[ExcludeFromCodeCoverage]
internal sealed record NodeAppearance(
    string Shape,
    string StrokeColor,
    string FillColor,
    double PortAnchorHalfWidth);
