using System.Collections.Generic;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// The top-level model for a node-graph canvas page.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the shape of <see cref="Components.IPageModel"/> (Id, Title, RenderMode) for consistency
/// with the FDW page-model family. The same canvas model is used for pipeline editing, lineage
/// viewing, and calculation-graph editing — the <see cref="RenderMode"/> determines which surface
/// is active.
/// </para>
/// <para>
/// Implementations must be render-agnostic (no Blazor/ASP.NET types).
/// Renderers receive this model and produce framework-specific output.
/// </para>
/// </remarks>
public interface ICanvasModel
{
    /// <summary>
    /// Gets the unique identifier for this canvas instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display title shown in the canvas chrome.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the current render mode (View or Edit).
    /// </summary>
    /// <remarks>
    /// Reuses the existing <see cref="IRenderMode"/> TypeCollection — no separate canvas-mode enum.
    /// Renderers check <see cref="IRenderMode.AllowsEditing"/> to decide whether to surface
    /// the <see cref="ICanvasEditContext"/>.
    /// </remarks>
    IRenderMode RenderMode { get; }

    /// <summary>
    /// Gets all nodes in the canvas.
    /// </summary>
    IReadOnlyList<ICanvasNode> Nodes { get; }

    /// <summary>
    /// Gets all edges in the canvas.
    /// </summary>
    IReadOnlyList<ICanvasEdge> Edges { get; }

    /// <summary>
    /// Gets the optional layout hint passed to the renderer (e.g. "dagre", "force", "manual").
    /// </summary>
    /// <remarks>
    /// A null value means the renderer chooses its default layout algorithm.
    /// No fallback — if a layout algorithm name is provided it must be meaningful.
    /// </remarks>
    string? LayoutHint { get; }

    /// <summary>
    /// Gets the optional currently selected node or edge identifier.
    /// </summary>
    /// <remarks>
    /// Null means nothing is selected. The renderer uses this to highlight the selected element.
    /// </remarks>
    string? SelectedId { get; }

    /// <summary>
    /// Gets the edit-mode command surface when the canvas is in an editable render mode.
    /// </summary>
    /// <remarks>
    /// Returns null when <see cref="RenderMode"/> does not allow editing
    /// (<see cref="IRenderMode.AllowsEditing"/> is false). Renderers and consumers
    /// must check for null before accessing edit operations.
    /// </remarks>
    ICanvasEditContext? EditContext { get; }
}
