using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Renderers.Svg;

namespace Fdw.UI.Canvas.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the built-in inline-SVG canvas renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: pan/zoom, node drag (edit mode), selection, connect and delete via the
/// <see cref="Fdw.UI.Abstractions.Canvas.ICanvasEditContext"/> — all without
/// third-party diagram libraries. Large graphs (&gt;~200 nodes) will degrade; set
/// <see cref="Fdw.UI.Abstractions.Canvas.ICanvasRendererType.SupportsLargeGraphs"/>
/// is <c>false</c>.
/// </para>
/// <para>
/// Ports are supported: each node's <see cref="Fdw.UI.Abstractions.Canvas.ICanvasPort"/>s render as
/// labelled discs in In/Out columns on the node's edges, the body grows to contain them, and edges
/// carrying port ids anchor to those discs. Clicking a source port then a target port draws a
/// <c>FieldMapping</c> edge between them — the gesture that authors a Map transform's field
/// mappings. This is the only renderer in the package with port support today; the
/// <see cref="BlazorDiagramsRendererType"/> and <see cref="CytoscapeRendererType"/> descriptors
/// still declare <c>SupportsPorts = false</c>.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="CanvasRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>CanvasHost</c> resolves the component straight from the
/// enumerable <see cref="CanvasRendererTypes"/> registry (auto-populated by the entry-point app's
/// Registration.SourceGenerators) — no separate map and no library module initializer.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasRendererTypes), "Svg")]
public sealed class SvgCanvasRendererType : CanvasRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SvgCanvasRendererType"/> class.
    /// </summary>
    public SvgCanvasRendererType()
        : base(
            id: 1,
            name: "Svg",
            displayName: "SVG",
            supportsEditing: true,
            supportsPorts: true,
            supportsLargeGraphs: false,
            layoutAlgorithms: ["manual"],
            renderComponentType: typeof(SvgCanvasRenderer))
    {
    }
}
