using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions;

/// <summary>
/// TypeCollection of pipeline task types.
/// Source generator populates all discovered <see cref="PipelineTaskTypeBase"/> options via
/// <c>[TypeOption(typeof(PipelineTaskTypes), ...)]</c> attributes.
/// </summary>
[TypeCollection(typeof(PipelineTaskTypeBase), typeof(IPipelineTaskType), typeof(PipelineTaskTypes))]
public sealed partial class PipelineTaskTypes : TypeCollectionBase<PipelineTaskTypeBase, IPipelineTaskType>
{
}
