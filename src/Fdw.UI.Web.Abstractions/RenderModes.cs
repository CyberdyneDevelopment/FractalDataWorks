using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Collection of render modes for web components.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WebRenderModeBase), typeof(IRenderMode), typeof(RenderModes))]
public abstract partial class RenderModes : TypeCollectionBase<WebRenderModeBase, IRenderMode>
{
}

