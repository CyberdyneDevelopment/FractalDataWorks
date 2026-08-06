using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// TypeCollection of available UI renderers.
/// </summary>
/// <remarks>
/// <para>
/// Provides compile-time discovery and O(1) lookup for UI renderer types.
/// Source generator creates static properties for each registered renderer.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Get renderer by name
/// var spectreRenderer = UIRenderers.ByName("Spectre");
///
/// // Get renderer by id
/// var renderer = UIRenderers.ById(1);
///
/// // Get all renderers
/// foreach (var r in UIRenderers.All()) { ... }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(UIRendererBase), typeof(IUIRendererType), typeof(UIRenderers))]
public sealed partial class UIRenderers : TypeCollectionBase<UIRendererBase, IUIRendererType>
{
}
