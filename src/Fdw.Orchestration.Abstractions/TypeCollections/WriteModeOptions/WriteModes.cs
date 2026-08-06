using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions;

/// <summary>
/// TypeCollection for write modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for write modes.
/// Source generator creates static properties for each registered write mode.
/// </remarks>
[TypeCollection(typeof(WriteModeBase), typeof(IWriteMode), typeof(WriteModes))]
public sealed partial class WriteModes : TypeCollectionBase<WriteModeBase, IWriteMode>
{
}
