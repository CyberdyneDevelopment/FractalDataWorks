using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// TypeCollection for canvas renderer types — the enumerable renderer registry.
/// </summary>
/// <remarks>
/// <para>
/// This TypeCollection is the renderer registry. Because <see cref="All"/> is source-generated,
/// the UI can enumerate every registered renderer without reflection for a selection dropdown.
/// <see cref="ByName"/> provides O(1) get-by-name for activating a renderer by its registry key.
/// No ServiceTypeCollection is used here: canvas renderers are stateless strategy objects, not
/// service instances that require three-phase DI registration or per-instance configuration.
/// This matches exactly how <see cref="Rendering.UIRenderers"/> works for the existing UI renderer
/// family in this package.
/// </para>
/// <para>
/// Usage (dropdown population):
/// <code>
/// var availableRenderers = CanvasRendererTypes.All();
/// // Bind to a select list — user picks a renderer by DisplayName
/// </code>
/// </para>
/// <para>
/// Usage (get by name after selection):
/// <code>
/// var rendererType = CanvasRendererTypes.ByName(selectedName);
/// if (rendererType == CanvasRendererTypes.NotFound)
///     // fail loud — the selected name is not a registered renderer
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(CanvasRendererTypeBase), typeof(ICanvasRendererType), typeof(CanvasRendererTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class CanvasRendererTypes : TypeCollectionBase<CanvasRendererTypeBase, ICanvasRendererType>
{
}
