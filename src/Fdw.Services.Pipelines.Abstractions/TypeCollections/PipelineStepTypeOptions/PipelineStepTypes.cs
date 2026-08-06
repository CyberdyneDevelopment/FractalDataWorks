using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions;

/// <summary>
/// TypeCollection for pipeline step types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(PipelineStepTypeBase), typeof(IPipelineStepType), typeof(PipelineStepTypes))]
public sealed partial class PipelineStepTypes : TypeCollectionBase<PipelineStepTypeBase, IPipelineStepType>
{
}
