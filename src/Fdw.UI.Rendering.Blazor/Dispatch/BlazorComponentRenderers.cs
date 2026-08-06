using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>
/// TypeCollection of model-to-Blazor-component mappings used to dispatch component rendering.
/// </summary>
/// <remarks>
/// Replaces the closed <c>switch (Model)</c> that previously decided which primitive painted a
/// component model. Resolve with <see cref="BlazorComponentRendererExtensions.ResolveFor"/>, which
/// applies <see cref="IBlazorComponentRenderer.Precedence"/> so concrete mappings beat the
/// interface-level fallbacks that would also match.
/// </remarks>
[TypeCollection(typeof(BlazorComponentRendererBase), typeof(IBlazorComponentRenderer), typeof(BlazorComponentRenderers))]
public sealed partial class BlazorComponentRenderers : TypeCollectionBase<BlazorComponentRendererBase, IBlazorComponentRenderer>
{
}
