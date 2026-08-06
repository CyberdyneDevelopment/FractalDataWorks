using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Renderers.NodeGraph;

namespace Fdw.UI.Canvas.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the Z.Blazor.Diagrams interactive canvas renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: interactive node graph powered by <c>Z.Blazor.Diagrams</c> — built-in pan/zoom,
/// node drag in edit mode, and link-drawing affordances.  Large graphs (&gt;~150 nodes) will
/// degrade in performance; <see cref="ICanvasRendererType.SupportsLargeGraphs"/> is <c>false</c>.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="CanvasRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>CanvasHost</c> resolves the component straight from the
/// enumerable <see cref="CanvasRendererTypes"/> registry — no separate map required.
/// </para>
/// <para>
/// The consuming application must include the Z.Blazor.Diagrams stylesheet at
/// <c>_content/Z.Blazor.Diagrams/style.min.css</c> in its host page or layout.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasRendererTypes), "Diagrams")]
public sealed class BlazorDiagramsRendererType : CanvasRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorDiagramsRendererType"/> class.
    /// </summary>
    public BlazorDiagramsRendererType()
        : base(
            id: 3,
            name: "Diagrams",
            displayName: "Blazor Diagrams",
            supportsEditing: true,
            supportsPorts: false,
            supportsLargeGraphs: false,
            layoutAlgorithms: ["manual"],
            renderComponentType: typeof(BlazorDiagramsRenderer))
    {
    }
}
