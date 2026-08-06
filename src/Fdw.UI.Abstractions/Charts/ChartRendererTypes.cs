using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// TypeCollection for chart renderer types — the enumerable renderer registry.
/// </summary>
/// <remarks>
/// <para>
/// This TypeCollection is the renderer registry. Because <see cref="All"/> is source-generated,
/// the UI can enumerate every registered renderer without reflection for a selection dropdown.
/// <see cref="ByName"/> provides O(1) get-by-name for activating a renderer by its registry key.
/// No ServiceTypeCollection is used here: chart renderers are stateless strategy descriptors, not
/// service instances that require three-phase DI registration or per-instance configuration.
/// This matches exactly how <see cref="Canvas.CanvasRendererTypes"/> works for the canvas seam.
/// </para>
/// <para>
/// This package (Fdw.UI.Abstractions) ships NO renderer implementations —
/// renderer packages (e.g. Fdw.UI.Charts.Blazor.ApexCharts) register their own
/// <c>[TypeOption]</c> against this TypeCollection in their own assembly, loaded by the
/// entry-point app's <c>Registration.SourceGenerators</c> module initialiser.
/// </para>
/// <para>
/// Usage (dropdown population):
/// <code>
/// var availableRenderers = ChartRendererTypes.All();
/// // Bind to a select list — user picks a renderer by DisplayName
/// </code>
/// </para>
/// <para>
/// Usage (filter chart types to selected renderer):
/// <code>
/// var rendererType = ChartRendererTypes.ByName(selectedName);
/// if (rendererType == ChartRendererTypes.NotFound)
///     // fail loud
/// var compatibleCharts = rendererType.SupportedChartTypes.Count == 0
///     ? ChartTypes.All()
///     : ChartTypes.All().Where(ct =>
///         rendererType.SupportedChartTypes.Contains(ct.Name, StringComparer.Ordinal));
/// </code>
/// </para>
/// <para>
/// Usage (get by name after selection):
/// <code>
/// var rendererType = ChartRendererTypes.ByName(selectedName);
/// if (rendererType == ChartRendererTypes.NotFound)
///     // fail loud — the selected name is not a registered renderer
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(ChartRendererTypeBase), typeof(IChartRendererType), typeof(ChartRendererTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ChartRendererTypes : TypeCollectionBase<ChartRendererTypeBase, IChartRendererType>
{
}
