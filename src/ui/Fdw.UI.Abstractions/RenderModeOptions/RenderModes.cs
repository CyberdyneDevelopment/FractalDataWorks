using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// TypeCollection for component rendering modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for render modes.
/// Source generator creates static properties for each registered render mode.
/// </remarks>
[TypeCollection(typeof(RenderModeBase), typeof(IRenderMode), typeof(RenderModes))]
public sealed partial class RenderModes : TypeCollectionBase<RenderModeBase, IRenderMode>
{
}
