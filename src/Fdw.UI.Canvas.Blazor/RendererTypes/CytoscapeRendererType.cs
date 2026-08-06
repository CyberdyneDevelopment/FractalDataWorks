using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Renderers.Cytoscape;

namespace Fdw.UI.Canvas.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the Cytoscape.js canvas renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: large-graph rendering (Cytoscape handles thousands of nodes efficiently via WebGL
/// and virtual DOM), interactive pan/zoom and selection (built into Cytoscape), read-only (no edit
/// context integration). Editing is wired at id=2 so it follows the SVG renderer (id=1) in the
/// enumerable <see cref="CanvasRendererTypes"/> registry.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="CanvasRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>CanvasHost</c> resolves the component straight from the
/// enumerable <see cref="CanvasRendererTypes"/> registry (auto-populated by the entry-point app's
/// Registration.SourceGenerators) — no separate map and no library module initializer.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasRendererTypes), "Cytoscape")]
public sealed class CytoscapeRendererType : CanvasRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CytoscapeRendererType"/> class.
    /// </summary>
    public CytoscapeRendererType()
        : base(
            id: 2,
            name: "Cytoscape",
            displayName: "Cytoscape",
            supportsEditing: false,
            supportsPorts: false,
            supportsLargeGraphs: true,
            layoutAlgorithms: ["preset", "breadthfirst"],
            renderComponentType: typeof(CytoscapeRenderer))
    {
    }
}
