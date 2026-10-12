using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// TypeCollection for collection display modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for collection display modes.
/// Source generator creates static properties for each registered collection display mode.
/// </remarks>
[TypeCollection(typeof(CollectionDisplayModeBase), typeof(ICollectionDisplayMode), typeof(CollectionDisplayModes))]
public sealed partial class CollectionDisplayModes : TypeCollectionBase<CollectionDisplayModeBase, ICollectionDisplayMode>
{
}
