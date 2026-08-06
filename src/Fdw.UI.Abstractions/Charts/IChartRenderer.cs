using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Rendering;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Core contract for chart renderers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations translate an <see cref="IChartModel"/> into a framework-specific chart
/// (e.g. a Blazor ApexCharts component, an ECharts widget via JS interop, an SVG export,
/// a Spectre.Console sparkline). The same <see cref="IChartModel"/> can be rendered by any
/// registered implementation — the model has no knowledge of the renderer.
/// </para>
/// <para>
/// Renderer instances are obtained from the DI container or a factory. The
/// <see cref="RendererType"/> property links the instance back to its
/// <see cref="ChartRendererTypes"/> registry entry so the caller can inspect capability flags
/// without holding a separate type reference.
/// </para>
/// <para>
/// No Blazor or ASP.NET types appear in this interface — the chart contract layer is
/// render-agnostic. This mirrors <c>ICanvasRenderer</c> exactly.
/// </para>
/// </remarks>
public interface IChartRenderer
{
    /// <summary>
    /// Gets the registry entry that describes the capabilities of this renderer.
    /// </summary>
    IChartRendererType RendererType { get; }

    /// <summary>
    /// Renders the chart model to the output surface described by <paramref name="context"/>.
    /// </summary>
    /// <param name="model">The chart model to render.</param>
    /// <param name="context">
    /// The render context (carries render mode, theme, dimensions).
    /// Mirrors the <c>ICanvasRenderer.RenderCanvas</c> signature for consistency
    /// with the existing FDW render-agnostic seam.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="RenderResult"/> indicating success or failure with an error message.</returns>
    Task<RenderResult> RenderChart(
        IChartModel model,
        IRenderContext context,
        CancellationToken cancellationToken = default);
}
