using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Rendering;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Core contract for canvas renderers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations translate an <see cref="ICanvasModel"/> into a framework-specific visual canvas
/// (e.g. a Blazor Diagram component, a React Flow widget via JS interop, an SVG export).
/// The same <see cref="ICanvasModel"/> can be rendered by any registered implementation — the
/// model has no knowledge of the renderer.
/// </para>
/// <para>
/// Renderer instances are obtained from the DI container or a factory. The <see cref="RendererType"/>
/// property links the instance back to its <see cref="CanvasRendererTypes"/> registry entry so the
/// caller can inspect capability flags without holding a separate type reference.
/// </para>
/// <para>
/// No Blazor or ASP.NET types appear in this interface — the canvas contract layer is
/// render-agnostic.
/// </para>
/// </remarks>
public interface ICanvasRenderer
{
    /// <summary>
    /// Gets the registry entry that describes the capabilities of this renderer.
    /// </summary>
    ICanvasRendererType RendererType { get; }

    /// <summary>
    /// Renders the canvas model to the output surface described by <paramref name="context"/>.
    /// </summary>
    /// <param name="model">The canvas model to render.</param>
    /// <param name="context">
    /// The render context (carries render mode, theme, dimensions).
    /// Mirrors the <see cref="IUIRenderer.Render"/> signature for consistency with the
    /// existing FDW render-agnostic seam.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="RenderResult"/> indicating success or failure with an error message.</returns>
    Task<RenderResult> RenderCanvas(
        ICanvasModel model,
        IRenderContext context,
        CancellationToken cancellationToken = default);
}
