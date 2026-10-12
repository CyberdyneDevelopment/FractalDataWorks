using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Interface for canvas edge type options.
/// </summary>
/// <remarks>
/// Each edge type carries display metadata so renderers need no switch/if-else on type names.
/// Compare against <see cref="CanvasEdgeTypes.NotFound"/> — never compare against null.
/// </remarks>
public interface ICanvasEdgeType : ITypeOption<int, CanvasEdgeTypeBase>
{
    /// <summary>
    /// Gets the human-readable display name for this edge type (e.g. "Flow", "Reference").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the icon hint string or line-style hint passed to the renderer.
    /// </summary>
    /// <remarks>
    /// Convention between the domain and the renderer. The canvas contract layer does not interpret it.
    /// </remarks>
    string IconHint { get; }
}
