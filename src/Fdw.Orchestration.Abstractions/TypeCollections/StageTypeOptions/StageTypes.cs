using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;

/// <summary>
/// TypeCollection for pipeline stage types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for stage types.
/// Source generator creates static properties for each registered stage type.
/// </remarks>
[TypeCollection(typeof(StageTypeBase), typeof(IStageType), typeof(StageTypes))]
public sealed partial class StageTypes : TypeCollectionBase<StageTypeBase, IStageType>
{
}
